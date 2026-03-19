# Publish

- zorg dat versie van publish (local) en server hetzelfde zijn (10.0 in dit geval), oudere server versie werkt NIET.

- rmb op solution -> publish -> local folder
- kopieer alleen publish folder data via ftp naar server (plesk in dit geval) niet de folder zelf!


## plesk structuur
- domain/httpdocs/[alles uit de publish folder] (httpdocs is niet per se nodig)
- (optioneel) zorg dat php versie uit staat binnen subdomain
- zorg dat .net core toolkit het volgende eruit zien op basis van eerder gevolgde stappen
<img width="700" height="439" alt="image" src="https://github.com/user-attachments/assets/a0a63796-4245-460a-867a-8ee69c418dda" />

- restart eventueel de applicatie
- de app zou nu moeten werken (nog zonder database)




## errors en dergelijke
- opletten dat er geen dubbele wwwroot komt te staan bij het uploaden van bestanden.
- appsettings.Development.json -> appsettings.Production.json
- juiste user credentials gebruikt? (dev pakt van laravel project momenteel (is ivm eigen server) )
- mogelijk een export van de db importeren!
