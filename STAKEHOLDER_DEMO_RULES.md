# Stakeholder Demo

Dit document bevat een korte demo-flow om de Rules-functionaliteit te presenteren.

## Planning met voorkeuren over meerdere versies

**Voorbereiding**

1. Controleer dat er reisleiders zichtbaar zijn op `/travelleaders`.
2. Controleer dat er reizen zichtbaar zijn op `/journeys`.
3. Controleer dat je voorkeuren kunt beheren via `/travelleaders/preferences/{Id}`.

**Test**

1. Open `/travelleaders/preferences/{Id}` en voeg voor een reisleider een duidelijke voorkeur toe voor een bestaande reis.
2. Open `/planner/draft`, genereer een draft en noteer bij 2-3 reizen welke reisleiders zijn toegewezen (nulmeting).
3. Ga terug naar `/travelleaders/preferences/{Id}` en voeg een extra voorkeur toe of wijzig de eerdere voorkeur.
4. Genereer opnieuw een draft via `/planner/draft` en vergelijk met de nulmeting.

**Resultaat**

1. De toewijzingen in de tweede draft sluiten beter aan op de ingestelde voorkeuren; bij conflicten blijven andere actieve rules en beschikbaarheid bepalend voor de uiteindelijke planning.

## Instellingen

**Voorbereiding**

1. Controleer dat de rule `RequiredExperience` aanwezig is op `/rules`.
2. Controleer dat er in de data reisleiders met verschillende ervaring bestaan (bijv. laag en hoog aantal reizen).
3. Controleer dat er meerdere reizen zichtbaar zijn op `/journeys` zodat je verschil in draft kunt vergelijken.

**Test**

1. Open `/rules`, zoek op `RequiredExperience` en noteer de huidige waarde + weight.
2. Open `/planner/draft`, genereer een draft en noteer bij 2-3 reizen welke reisleiders nu zijn toegewezen (nulmeting).
3. Ga terug naar `/rules` en verhoog `RequiredExperience` duidelijk (bijv. van 2 naar 10) zodat minder reisleiders aan de eis voldoen.
4. Genereer opnieuw een draft via `/planner/draft` en vergelijk met de nulmeting.

**Resultaat**

1. Reisleiders met weinig ervaring worden minder of niet meer toegewezen, meer ervaren reisleiders krijgen relatief vaker toewijzingen of sommige reizen blijven (deels) onvervuld als er te weinig ervaren reisleiders beschikbaar zijn.
