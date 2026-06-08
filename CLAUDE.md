# Kaaiman Reizen — Developer Context

## Project

Blazor Server app (.NET 8) met MudBlazor + Bootstrap. MySQL database via EF Core. Rollen: **Planner** en **Reisleider**.

Serviceregistratie zit in `Kaaiman-reizen.Data/ServiceCollectionExtensions.cs` (data services) en `Kaaiman-reizen/Program.cs` (app services).

---

## Actieve feature: PlanningRound

We bouwen een systeem van **planningsrondes** zodat de planner meerdere onafhankelijke planningen per jaar kan maken (bijv. periode 1 en periode 2) en ook vooruit kan plannen naar een volgend jaar.

### Waarom

De huidige code publiceert altijd maar 1 planning per jaar — bij elke nieuwe publicatie wordt de vorige onzichtbaar. Dat moet veranderen.

---

## Volledige flow (zoals afgesproken)

```
1. Planner maakt reisleideraccounts aan  (/travelleaders)
2. Planner maakt alle reizen aan         (/journeys)
3. Planner klikt "Planningsronde aanmaken" op de reizenpagina
   → modal opent met:
     - Naam (auto: "Planningsronde 2026")
     - Startdatum / Einddatum (auto-afgeleid van vroegste/laatste Huidig-reis)
     - Voorkeur deadline  ← reisleiders moeten vóór deze datum hun voorkeur invullen
     - Publicatie deadline ← planner moet vóór deze datum publiceren
     - Live preview: welke reizen vallen in de datumreeks
   → Na opslaan: direct naar /planner/rounds/{id}/draft

4. Reisleiders zien op /travelleaders/preferences de openstaande ronde(s)
   → Per ronde kunnen zij hun voorkeur invullen (rank 1-3 + beschikbaar zonder voorkeur)
   → Na indienen: PlanningRoundParticipation.Status → Submitted

5. Planner genereert draft op /planner/rounds/{id}/draft
   → Draft is scoped aan de ronde (alleen reizen binnen StartDate–EndDate)
   → Context-balk bovenaan toont: naam, datumreeks, voorkeur deadline, X/Y ingediend
   → Planner kan handmatig toewijzingen aanpassen (drawer)
   → Planner kan opslaan als concept of publiceren

6. Publiceren
   → Maakt nieuwe PlanningVersion aan met PlanningRoundId gekoppeld
   → Ontheft alleen vorige gepubliceerde versies van DEZE ronde (niet andere rondes)
   → Stuurt notificaties + emails

7. Gepubliceerde planning aanpassen
   → Planner opent de gepubliceerde ronde via /planner/rounds
   → Kan direct toewijzingen bewerken en opslaan (geen nieuw concept nodig)
   → Bij opslaan: assignments direct updaten op bestaande PlanningVersion
   → Notificatie verstuurd bij elke opgeslagen wijziging
```

---

## Wat er al gebouwd is (deze sessie)

### Entities (migrations nog uitvoeren!)

| Entity | Bestand | Status |
|---|---|---|
| `PlanningRound` | `Kaaiman-reizen.Data/Entities/PlanningRound.cs` | ✅ Aangemaakt |
| `PlanningRoundParticipation` | `Kaaiman-reizen.Data/Entities/PlanningRoundParticipation.cs` | ✅ Aangemaakt |
| `PlanningRoundPreference` | `Kaaiman-reizen.Data/Entities/PlanningRoundPreference.cs` | ✅ Aangemaakt |
| `PlanningVersion` | bestaand | ✅ `PlanningRoundId` FK toegevoegd |
| `ParticipationStatus` enum | `Kaaiman-reizen.Data/Enum/ParticipationStatus.cs` | ✅ Aangemaakt |
| `MainContext` | bestaand | ✅ DbSets + relaties + unique indexes toegevoegd |

**Migratie nog uitvoeren:**
```
dotnet ef migrations add AddPlanningRound --project Kaaiman-reizen.Data --startup-project Kaaiman-reizen
dotnet ef database update --project Kaaiman-reizen.Data --startup-project Kaaiman-reizen
```

### Services

| Service | Status |
|---|---|
| `IPlanningRoundService` + `PlanningRoundService` | ✅ `CreateAsync` + `GetAllAsync` |
| Geregistreerd in `ServiceCollectionExtensions` | ✅ |

### UI

| Pagina/Component | Status |
|---|---|
| `PlanningRoundCreateModal.razor` (in Pages/Journeys/) | ✅ Volledig gebouwd |
| `Journeys.razor` — knop + modal | ✅ Knop + modal gekoppeld |
| `/planner/rounds` overzichtspagina | ❌ Nog niet gebouwd |
| `/planner/rounds/{id}/draft` | ❌ Begonnen, niet afgerond |
| `/travelleaders/preferences` — rondeselector | ❌ Nog niet gebouwd |
| NavMenu aanpassen | ❌ Nog niet gedaan |

---

## Wat nog gebouwd moet worden (volgende sessie)

### 1. Services uitbreiden

**`IPlannerDraftService` + `PlannerDraftService`**
- Overload toevoegen: `BuildRequestAsync(PlanningRound round, CancellationToken ct = default)`
- Filtert reizen op `round.StartDate <= j.Start && j.Start <= round.EndDate` i.p.v. jaar
- Leest voorlopig nog uit globale `PreferredDestination` (niet uit `PlanningRoundPreference`)

**`IPlanningService` + `PlanningService`**
- `GetDraftsByRoundAsync(int roundId, ...)` — drafts scoped aan ronde
- `GetPublishedByRoundAsync(int roundId, ...)` — gepubliceerde versie van ronde
- `SavePlanningForRoundAsync(int roundId, int year, string name, bool isPublished, assignments, ...)`:
  - Bij publish: ontheft alleen vorige gepubliceerde versies van DEZELFDE ronde (via `PlanningRoundId`)
  - Bij draft: update bestaande draft voor ronde of maak nieuwe aan
  - Zet `PlanningVersion.PlanningRoundId = roundId`

**`IPlanningRoundService` + `PlanningRoundService`**
- `GetByIdAsync(int id, ...)` — laad ronde met participaties

### 2. `/planner/rounds/{id}/draft` (PlannerRoundDraft)

Nieuwe pagina als round-scoped versie van de bestaande `/planner/draft`.

**Bestanden aanmaken:**
- `PlannerRoundDraft.razor` — `@page "/planner/rounds/{RoundId:int}/draft"`
- `PlannerRoundDraft.razor.cs`
- `PlannerRoundDraft.Save.cs`
- `PlannerRoundDraft.Candidates.cs` (identiek aan `PlannerDraft.Candidates.cs`)
- `PlannerRoundDraft.razor.css` (identiek aan `PlannerDraft.razor.css`)

**Context-balk bovenaan:**
```
◀ Rondes  |  Periode 1 2026  |  1 jan – 30 jun  |  Voorkeur: 15 mrt (11 dgn)  |  18/22 ingediend
```
- Kleur deadline: grijs als ver weg, oranje als < 7 dagen, rood als verlopen
- Kleur participatie: groen als alles ingediend, oranje als deels, rood als weinig

**Verschillen t.o.v. PlannerDraft:**
- Route parameter `RoundId` i.p.v. jaar
- `LoadDataForRoundAsync` i.p.v. `LoadDataForYearAsync`
- Gebruikt `BuildRequestAsync(round)` overload
- Gebruikt `GetDraftsByRoundAsync` / `SavePlanningForRoundAsync`
- Geen "Nieuwe planningsperiode starten" knop
- Auto-laadt meest recente draft of genereert direct (geen EntryModal)
- "← Terug naar rondes" navigatieknop

**Gepubliceerde planning aanpassen:**
- Als de ronde al een gepubliceerde versie heeft → laad die als startpunt
- Toon waarschuwingsbanner: "Dit is de gepubliceerde planning. Wijzigingen zijn direct zichtbaar."
- Opslaan → update assignments op bestaande `PlanningVersion` (geen nieuwe versie)
- Notificaties versturen bij opslaan van wijzigingen

### 3. `/planner/rounds` — Rondes overzicht

Nieuwe pagina als landing point voor "Planningsrondes" in de nav.

**Cards per ronde:**
```
┌─────────────────────────────────────────┐
│ Periode 1 2026                  ● Open  │
│ 8 reizen  ·  Deadline: 15 mrt (11 dgn) │
│ Deelname: 18 / 22 ingediend             │
│                        [Open draft →]   │
└─────────────────────────────────────────┘
```

**Status badge (berekend, niet opgeslagen):**
- `Open` — nu < PreferenceDeadline
- `Voorkeuren gesloten` — PreferenceDeadline verstreken, nog niet gepubliceerd
- `Gepubliceerd` — heeft PlanningVersion met IsPublished = true

**Acties per ronde:**
- Open: [Open draft →]
- Gepubliceerd: [Bekijk] + [Aanpassen]

### 4. `/travelleaders/preferences` — Rondeselector toevoegen

Update bestaande pagina om per ronde voorkeuren in te vullen.

- Bovenaan: lijst van openstaande rondes (status = Open/PreferencesClosed, participation = Pending)
- Geselecteerde ronde bepaalt welke reizen getoond worden (filter op round.StartDate–EndDate)
- Na opslaan: `PlanningRoundParticipation.Status = Submitted`, `SubmittedAt = DateTime.UtcNow`
- Sla voorkeuren op in `PlanningRoundPreference` (nieuw) i.p.v. globale `PreferredDestination`

### 5. NavMenu aanpassen

```
/planner/rounds   "Planningsrondes"   (vervangt /planner/draft)
```

---

## Ontwerpbeslissingen (vastgesteld)

| Beslissing | Keuze |
|---|---|
| Meerdere gepubliceerde planningen | Ja — per ronde onafhankelijk |
| Gepubliceerde planning aanpassen | Direct bewerken (geen nieuwe versie), wel notificatie |
| Ronde aanmaken | Via modal op /journeys, redirect naar draft |
| Reizen in ronde bepalen | Datumreeks (StartDate–EndDate op PlanningRound) |
| Voorkeur opslaan | Nieuw: PlanningRoundPreference per ronde (i.p.v. globale PreferredDestination) |
| Preference deadline blokkering | Zacht — waarschuwing, geen hard blokkeren |
| Participatie bij ronde aanmaken | Direct alle actieve reisleiders als Pending aanmaken |
| Historische voorkeuren | Bewaard via PlanningRoundPreference per ronde (archief overbodig) |

---

## Bestaande code om NIET te breken

- `/planner/draft` (bestaande pagina) — blijft gewoon werken
- `SavePlanningAsync` bestaande overload — niet aanpassen, nieuwe methoden toevoegen
- `BuildRequestAsync(int year)` — niet aanpassen, overload toevoegen
- `TravelLeaderAvailabilityHistory` archiefsysteem — blijft bestaan voor legacy data
