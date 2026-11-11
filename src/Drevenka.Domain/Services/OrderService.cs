using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;

namespace Drevenka.Domain.Services;

public record CreateOrderItemDto(int ProductId, int Quantity);

public record CreateOrderDto(
    int CustomerId,
    string DeliveryAddress,
    string DeliveryCity,
    string DeliveryPostalCode,
    string? CustomerNote,
    List<CreateOrderItemDto> Items);

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetAllOrdersAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken ct = default);
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task<Order?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<Order> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task DispatchOrderAsync(int orderId, string performedBy, CancellationToken ct = default);
    Task CancelOrderAsync(int orderId, string reason, CancellationToken ct = default);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockRepository _stockRepository;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository,
        IStockRepository stockRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _stockRepository = stockRepository;
    }

    public async Task<IReadOnlyList<Order>> GetAllOrdersAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken ct = default)
    {
        return await _orderRepository.GetAllAsync(status, searchTerm, ct);
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        return await _orderRepository.GetByIdAsync(id, ct);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await _orderRepository.GetByOrderNumberAsync(orderNumber, ct);
    }

    public async Task<Order> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new DomainException("Objednávka musí obsahovat alespoň jednu položku.");
        }

        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, ct)
            ?? throw new DomainException($"Zákazník s ID {dto.CustomerId} nebyl nalezen.");

        if (!customer.IsActive)
        {
            throw new DomainException($"Nelze vytvořit objednávku pro neaktivního zákazníka '{customer.FullName}'.");
        }

        var mainWarehouse = await _warehouseRepository.GetMainWarehouseAsync(ct)
            ?? (await _warehouseRepository.GetAllAsync(onlyActive: true, ct)).FirstOrDefault()
            ?? throw new DomainException("V systému není evidován žádný aktivní expediční sklad.");

        var orderNumber = await _orderRepository.GenerateNextOrderNumberAsync(ct);

        var order = new Order(
            orderNumber: orderNumber,
            customerId: customer.Id,
            deliveryAddress: dto.DeliveryAddress,
            deliveryCity: dto.DeliveryCity,
            deliveryPostalCode: dto.DeliveryPostalCode,
            customerNote: dto.CustomerNote);

        // 1. Validace dostupnosti zásob a okamžitá rezervace
        var reservedStockItems = new List<(StockItem Item, int Qty, decimal UnitPrice)>();

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
            {
                throw new DomainException("Objednané množství musí být alespoň 1 ks.");
            }

            var product = await _productRepository.GetByIdAsync(itemDto.ProductId, ct)
                ?? throw new DomainException($"Produkt s ID {itemDto.ProductId} nebyl nalezen.");

            if (!product.IsActive)
            {
                throw new DomainException($"Produkt '{product.Name}' není aktivní k prodeji.");
            }

            var stockItem = await _stockRepository.GetOrCreateStockItemAsync(product.Id, mainWarehouse.Id, ct);

            // Klient vyžaduje: Systém musí odmítnout objednávku, na kterou není dost zásob
            if (stockItem.AvailableQuantity < itemDto.Quantity)
            {
                throw new InsufficientStockException(product.Name, itemDto.Quantity, stockItem.AvailableQuantity);
            }

            stockItem.ReserveStock(itemDto.Quantity);
            reservedStockItems.Add((stockItem, itemDto.Quantity, product.SellingPrice));
            order.AddItem(product.Id, itemDto.Quantity, product.SellingPrice);
        }

        // 2. Potvrzení objednávky s rezervovanými zásobami
        order.Confirm();

        await _orderRepository.AddAsync(order, ct);
        await _stockRepository.SaveChangesAsync(ct);

        return order;
    }

    public async Task DispatchOrderAsync(int orderId, string performedBy, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct)
            ?? throw new DomainException($"Objednávka s ID {orderId} nebyl nalezena.");

        if (order.Status != OrderStatus.Confirmed)
        {
            throw new DomainException($"Objednávku ve stavu '{order.Status}' nelze expedovat. Expedovat lze pouze potvrzenou objednávku.");
        }

        var mainWarehouse = await _warehouseRepository.GetMainWarehouseAsync(ct)
            ?? (await _warehouseRepository.GetAllAsync(onlyActive: true, ct)).FirstOrDefault()
            ?? throw new DomainException("V systému není evidován žádný aktivní expediční sklad.");

        foreach (var item in order.Items)
        {
            var stockItem = await _stockRepository.GetOrCreateStockItemAsync(item.ProductId, mainWarehouse.Id, ct);
            var previousQty = stockItem.QuantityOnHand;

            // Odpíše fyzickou zásobu a zruší rezervaci
            stockItem.ShipReserved(item.Quantity);
            var newQty = stockItem.QuantityOnHand;

            var movement = new StockMovement(
                stockItemId: stockItem.Id,
                productId: item.ProductId,
                warehouseId: stockItem.WarehouseId,
                movementType: StockMovementType.Issue,
                quantity: item.Quantity,
                previousQuantity: previousQty,
                newQuantity: newQty,
                note: $"Fyzická expedice zákaznické objednávky č. {order.OrderNumber} pro {order.Customer?.FullName ?? "zákazníka"}",
                referenceNumber: order.OrderNumber,
                performedBy: performedBy);

            await _stockRepository.AddMovementAsync(movement, ct);
        }

        order.Dispatch();
        await _orderRepository.UpdateAsync(order, ct);
        await _stockRepository.SaveChangesAsync(ct);
    }

    public async Task CancelOrderAsync(int orderId, string reason, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct)
            ?? throw new DomainException($"Objednávka s ID {orderId} nebyla nalezena.");

        if (order.Status == OrderStatus.Dispatched)
        {
            throw new DomainException("Již expedovanou objednávku nelze stornovat.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return;
        }

        var mainWarehouse = await _warehouseRepository.GetMainWarehouseAsync(ct)
            ?? (await _warehouseRepository.GetAllAsync(onlyActive: true, ct)).FirstOrDefault()
            ?? throw new DomainException("V systému není evidován žádný aktivní expediční sklad.");

        // Pokud byla objednávka potvrzená, uvolníme rezervace
        if (order.Status == OrderStatus.Confirmed)
        {
            foreach (var item in order.Items)
            {
                var stockItem = await _stockRepository.GetOrCreateStockItemAsync(item.ProductId, mainWarehouse.Id, ct);
                stockItem.ReleaseReservation(item.Quantity);
            }
        }

        order.Cancel(reason);
        await _orderRepository.UpdateAsync(order, ct);
        await _stockRepository.SaveChangesAsync(ct);
    }
}

