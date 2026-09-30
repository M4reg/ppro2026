# AGENTS.md – Pravidla a zásady pro AI asistenta

Tento dokument definuje závazná pravidla chování, standardy a pracovní postupy pro AI asistenta (Antigravity / Gemini) při vývoji semestrálního projektu z předmětu **Pokročilé programování (PPRO), ZS 2026/2027**.

Projekt: **Zadání B – Sklad pro malý e-shop (Dřevěnka s.r.o.)**  
Jediný zdroj pravdy projektu: [README.md](file:///u:/ppro2026/README.md)

---

## 1. Zásady práce s Git a verzováním

1. **Striktní formátování commitů (`-m`)**:
   - Každý commit musí být proveden výhradně s přepínačem `-m` a věcnou, strukturovanou zprávou.
   - Používej konvenci *Conventional Commits* s českým nebo anglickým popisem:
     - `feat: <popis nové funkcionality>`
     - `fix: <oprava chyby>`
     - `docs: <úpravy v README.md, AGENTS.md či jiné dokumentaci>`
     - `refactor: <úprava kódu beze změny chování>`
     - `test: <přidání či oprava testů>`
     - `chore: <konfigurace, Docker, migrace, závislosti>`
   - Commit zpráva musí přesně vystihovat podstatu změn.

2. **Absolutní zákaz automatického pushování**:
   - **NIKDY neprováděj `git push` automaticky ani z vlastní iniciativy.**
   - Příkaz `git push` smí být spuštěn **výhradně na základě explicitního povelu uživatele** (např. *"pushni to"*, *"nahraj změny na GitHub"*, *"udělej push"*).
   - V případě pochybností se uživatele zeptej, zda si přeje změny odeslat do vzdáleného repozitáře.

3. **Synchronizace s technickou dokumentací před commitem**:
   - `README.md` je **jediný zdroj pravdy (Single Source of Truth)**.
   - **Před každým commitem** se asistent MUSÍ ujistit, že:
     - Všechny provedené architektonické nebo implementační změny jsou zachyceny v [README.md](file:///u:/ppro2026/README.md).
     - Byla zkontrolována a případně aktualizována sekce **Rozhodnutí** a **Deník změn (Changelog)**.
     - V repozitáři jsou aktivní Git hooky v `.githooks/`, které toto pravidlo pomáhají vynucovat.

---

## 2. Standardy semestrálního projektu PPRO

1. **Třívrstvá architektura**:
   - Závislosti směřují striktně jedním směrem:  
     `Prezentační vrstva (UI / API)` ➔ `Aplikační / Doménová vrstva (Business Logic)` ➔ `Datová vrstva (Persistence / DB)`.
   - Obchodní pravidla nesmí protékat do UI ani do databázových triggerů bez zapouzdření v doméně.

2. **Relační databáze a migrace**:
   - Relační databáze běží v kontejneru v Dockeru.
   - Jakákoliv změna schématu musí být realizována přes verzované migrace (žádné ruční ad-hoc úpravy schématu).

3. **Spuštění projektu**:
   - Celá aplikace včetně databáze musí být spustitelná jediným příkazem:
     ```bash
     docker compose up
     ```

4. **Syntetická data a bezpečnost**:
   - Žádná reálná data, žádná osobní data (PII) reálných osob.
   - Žádné citlivé přihlašovací údaje (hesla, tokeny, privátní klíče) nesmí být uloženy v repozitáři.

5. **Testování**:
   - Klíčová obchodní pravidla zadání musí být pokryta automatickými testy (jednotkovými/integračními).

---

## 3. Klíčová obchodní pravidla Zadání B (Sklad e-shopu)

Při každé implementaci musí asistent hlídat a vynucovat:
1. **Zákaz výdeje do záporu**: Nelze vydat více zboží, než je aktuálně fyzicky skladem.
2. **Neměnnost historických cen**: Úprava ceníku produktu se nesmí projevit na položkách již dříve odeslaných/vytvořených objednávek.
3. **Auditovatelnost každého pohybu**: Každá změna stavu zásob (příjem, výdej, přesun, inventurní manko/přebytek) musí mít odpovídající neměnný záznam ve skladových pohybech.
4. **Zákaz mazání zákazníků**: Zákazník se v systému nemaže kvůli vazbám na objednávky a reklamace (povoleno pouze soft delete / archivace, pokud vůbec).
5. **Rozlišení Fyzické vs. Disponibilní zásoby**: Okamžik rezervace probíhá při přijetí objednávky; fyzický odpis ze skladu probíhá při expedici.

---

## 4. Komunikační styl a výstupy

- Komunikace probíhá v češtině (nebo jazyce uživatele).
- Kód a komentáře jsou psány čistě, přehledně a s respektem k zavedeným konvencím zvoleného jazyka/frameworku.
- Odkazy na soubory uváděj vždy ve formátu markdown odkazů s protokolem `file:///` (např. `[README.md](file:///u:/ppro2026/README.md)`).
