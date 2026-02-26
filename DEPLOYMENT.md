# Publish

- zorg dat versie van publish (local) en server hetzelfde zijn (10.0 in dit geval), oudere server versie werkt NIET.

- rmb op solution -> publish -> local folder
- kopieer alleen publish folder data via ftp naar server (plesk in dit geval) niet de folder zelf!


## plesk structuur
- doamin/httpdocs/[alles uit de publish folder]
- (optioneel) zorg dat php versie uit staat binnen subdomain
- zorg dat .net core toolkit het volgende eruit zien op basis van eerder gevolgde stappen
<img width="700" height="439" alt="image" src="https://github.com/user-attachments/assets/a0a63796-4245-460a-867a-8ee69c418dda" />

- restart eventueel de applicatie
- de app zou nu moeten werken (nog zonder database)
