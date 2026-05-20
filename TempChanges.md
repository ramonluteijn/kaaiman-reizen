Ticket is geïmplementeerd en opnieuw gevalideerd.

Wat is aangepast

1. Homepagina datumformaat gecorrigeerd naar de centrale formattering dd-MM-yyyy:
Home.razor 138

2. Consistente datumformattering toegepast op resterende plekken met hardcoded datumstrings:
TravelLeaders.razor 66
TravelHistory.razor 45
PlannerDraft.razor 85

3. Timezone/UTC testen toegevoegd:
DateDisplayTests.cs 8
- Controle op exact formaat dd-MM-yyyy
- UTC offset-conversie
- Specifieke case voor DateTimeKind.Unspecified (database-materialisatie scenario)

4. Database UTC test toegevoegd voor CreatedAt-opslag:
PlanningServiceTests.cs 104
- Verifieert dat CreatedAt als UTC wordt opgeslagen in PlanningVersion

Hercontrole

- Gezocht op resterende hardcoded datumformaten zoals dd MMM en dd-MM-yyyy ToString; geen resterende matches in app-code.
- Planning dashboard, reisoverzichten en PDF-export gebruiken nu overal dezelfde DateDisplay-lijn.

Testresultaat

- Test run geslaagd: 34 totaal, 34 geslaagd, 0 gefaald, 0 overgeslagen.
- Tijdens eerste run blokkeerde een draaiend proces de build (MSB3027/MSB3021); na stoppen van dat proces draaiden alle tests succesvol.

Mogelijke volgende stap

1. Als je wilt, kan ik nog een korte handmatige UI-checklist uitvoeren per scherm (Home, Planner, Reisoverzichten, PDF) met concrete testscenario’s voor verschillende tijdzones.