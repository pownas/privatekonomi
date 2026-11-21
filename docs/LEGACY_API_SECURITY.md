# Äldre API är spärrat

De äldre API-controllerna är av säkerhetsskäl spärrade tills de har migrerats med behörighetskontroll och isolering av användardata. De returnerar normalt HTTP 403 även för en giltig personlig bearer-token. En token för det personliga API:et innebär inte generell behörighet. `api-info`, inloggningen och det nya personliga transaktions-API:et påverkas inte.

Det påverkar klienter som använder de äldre API:erna, inklusive webbens bankkopplingar och notifieringsåtgärder när de anropar API-tjänsten. Backup/export/import i datahantering, export av lånestrategier och service workerns synkronisering av transaktioner påverkas också om deras URL:er routas till API-tjänsten. Funktioner som använder interna tjänster direkt omfattas inte av denna spärr.

Hela bankkopplings-API:et är spärrat, även OAuth-callbacken. Pågående bankauktoriseringar kan därför inte slutföras och callbacken omdirigerar inte med auktoriseringskoden. Återaktivera inte callbacken separat innan hela flödet har säker användarbindning och verifierad state-hantering.

Äldre controllerfunktionstester körs med ett särskilt testvärdundantag; produktionsvärden har ingen sådan inställning. Nya controllers ska klassificeras uttryckligen och måste ha säker autentisering, auktorisering och dataisolering innan de exponeras.
