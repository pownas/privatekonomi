# Avstämning – REST API-profil v2.0.0 (DIGG)

> **Källa:** [Sveriges dataportal – REST API-profil (Versionshantering)](https://www.dataportal.se/rest-api-profil/versionhantering)  
> **Profil:** [REST API-profil v2.0.0](https://dev.dataportal.se/rest-api-profil) (publicerad juni 2026)  
> **Statusvärden:** `-` (Ej bedömt) | `OK` (Uppfyllt) | `Pågående` (Påbörjat) | `NOK` (Ej uppfyllt) | `N/A` (Ej applicerbart)

## [Dokumentation](https://dev.dataportal.se/rest-api-profil/dokumentation)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `DOK.01` | I regel BÖR dokumentationen och specifikationen för ett API finnas allmänt tillgänglig online. | **BÖR** | - |  |
| `DOK.02` | Vidare BÖR ett API:s dokumentation och specifikation finnas sökbara via Sveriges dataportal. | **BÖR** | - |  |
| **API dokumentation** | | | | |
| `DOK.03` | Dokumentationen för ett API SKALL innehålla följande:<br>• Om API<br>• Användarvillkor<br>• Datamodell för representation av resurser<br>• Krav på autentisering<br>• Livscykelhantering och versionshantering<br>• Kontaktuppgifter | **SKALL** | - |  |
| `DOK.04` | Dokumentationen och i synnerhet specifikationen SKALL ses som ett kontrakt mellan designer och utvecklare samt mellan producent och konsument av API:et. | **SKALL** | - |  |
| `DOK.05` | Dokumentationen SKALL versionshanteras tillsammans med API:et. | **SKALL** | - |  |
| `DOK.06` | Dokumentationen BÖR finnas på både svenska och engelska. | **BÖR** | - |  |
| `DOK.07` | Dokumentationen av ett API BÖR innehålla övergripande information om API:et. | **BÖR** | - |  |
| `DOK.08` | Ett API:s servicenivå SKALL finnas tydligt beskrivna i dokumentationen. | **SKALL** | - |  |
| `DOK.09` | Kända problem och begränsningar SKALL finnas tydlig beskrivning dokumentationen. | **SKALL** | - |  |
| `DOK.10` | Om det är känt SKALL tidpunkt för när API:et tas ur bruk anges i dokumentationen. | **SKALL** | - |  |
| `DOK.11` | Avsikten och beteendet hos API:et SKALL beskrivas så utförligt och tydligt som möjligt. | **SKALL** | - |  |
| `DOK.12` | Ett API:s resurser och de möjliga operationer som kan utföras på resursen SKALL beskrivas så utförligt och tydligt som möjligt. | **SKALL** | - |  |
| `DOK.13` | Förväntade returkoder och felkoder SKALL vara fullständigt dokumenterade. | **SKALL** | - |  |
| `DOK.15` | I dokumentationen av API:et SKALL exempel på API:ets fråga (en:request) och svar (en:reply) finnas i sin helhet. | **SKALL** | - |  |
| **API specifikation** | | | | |
| `DOK.16` | REST API:er SKALL ha en API specifikation som bör kunna generera en datamodell för representation av API:ets resurser. | **SKALL** | - |  |
| `DOK.17` | API specifikation BÖR dokumenteras med den senaste major-versionen av OpenAPI Specification. | **BÖR** | - |  |
| `DOK.18` | API specifikationen BÖR beskrivas med antingen JSON eller YAML. | **BÖR** | - |  |
| `DOK.19` | Ett API:s resurser och de möjliga operationer som kan utföras på resursen SKALL beskrivas så utförligt och tydligt som möjligt. | **SKALL** | - |  |
| `DOK.20` | Förväntade returkoder och felkoder SKALL vara fullständigt dokumenterade. | **SKALL** | - |  |
| `DOK.21` | Krav på autentisering SKALL anges i specifikationen. | **SKALL** | - |  |
| `DOK.22` | Varje ny major-version av ett API, enligt versionshanteringsavsnittet, SKALL följas av en ny API specifikation. | **SKALL** | - |  |
| `DOK.23` | API specifikationen SKALL återfinnas under API-roten, det vill säga:<br>{protokoll}://{domännamn}/{API}/{version}/ | **SKALL** | - |  |
| `DOK.24` | Om API specifikationen specificeras med OpenAPI SKALL roten till specifikationen namnges med openapi.yaml eller openapi.json. | **SKALL** | - |  |

## [Datum- och tidsformat](https://dev.dataportal.se/rest-api-profil/datum-och-tidsformat)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `DOT.01` | Datum och tid SKALL hanteras enligt följande:<br>• Använd alltid RFC 3339 för datum och tid<br>• Acceptera alla tidszoner i API:er<br>• Returnera datum och tid i UTC<br>• Använd inte tidsdelen om du inte behöver den | **SKALL** | - |  |
| **Datum och tid** | | | | |
| `DOT.02` | Datum och tid SKALL anges enligt RFC 3339 som bygger på ISO-8601. | **SKALL** | - |  |
| **Tidszoner** | | | | |
| `DOT.03` | När man använder RFC 3339 format BÖR tidszonen anges. | **BÖR** | - |  |
| `DOT.04` | Tidzonen BÖR representeras med UTC formatet, där tid anges som offset från UTC (Coordinated Universal Time). | **BÖR** | - |  |

## [Resurser](https://dev.dataportal.se/rest-api-profil/resurser)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| **Identifierare** | | | | |
| `RES.02` | Primärnycklar eller personligt identifierbar information (personnummer, etc) BÖR INTE exponeras. | **BÖR INTE** | - |  |
| `RES.03` | När numeriska ID:n används BÖR de INTE vara sekventiella. Om informationen är av känslig karaktär ska den skyddas med behörighetskontroll. | **BÖR INTE** | - |  |
| `RES.04` | En identifierare för en nästlad resurs SKALL vara unik inom sin förälder resurs om den är beroende av sin förälder. | **SKALL** | - |  |
| `RES.05` | När det kommer till att identifiera resurser så BÖR designen av detta ta höjd för en del olika aspekter:<br>• Säkerhet – beakta den identifikation på resurs som förmedlas via API gällande om den kan anses vara känslig eller onödig att exponera. T.ex. består en identifierare av en räknare så är det enkelt att lista ut nästa resurs. Avstå också från att använda primärnycklar i databasen utåt.<br>• Användandet av logiska nycklar – den logiska nyckeln används i exponering utåt medan relationer döljs i backend. Passar bra när resurserna tillhör en domän, såsom SEK tillhör domänen valutor.<br>• Överväg användande av UUID (eller motsvarande) – likt de två ovanstående punkterna så används databasgenererade id:n för att hantera data i backend, medan den logiska nyckeln döljs genom att skapa en UUID (t.ex. genom MD5 hashning av logisk nyckel). UUID kan såklart också genereras trots att det inte finns en logisk nyckel att använda. | **BÖR** | - |  |
| **Namnsättningskonvention för resurser** | | | | |
| `RES.06` | Följande regler för resurser SKALL följas:<br>• Resurser namnges som substantiv.<br>• Resurser namnges i pluralform, även om den nu kända träffbilden bara ger ett resultat<br>• Resurser följer den namnsättningskonvention som beskrivs för URL:er, det vill säga att resurser anges med gemener, använder endast alfanumeriska tecken och bindestreck för att separera eventuella ord. | **SKALL** | - |  |

## [URL format och namngivning](https://dev.dataportal.se/rest-api-profil/url-format-och-namngivning)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| **URL komponenter** | | | | |
| `UFN.01` | En URL för ett API BÖR följa namnstandarden nedan:<br>{protokoll}://{domännamn }/{api}/{version}/{resurs}/{identifierare}?{parametrar} | **BÖR** | - |  |
| `UFN.02` | Alla API:er SKALL exponeras via HTTPS på port 443. | **SKALL** | - |  |
| `UFN.03` | Parametrar i sökfrågor SKALL INTE användas för att transportera payload eller faktiska data. | **SKALL INTE** | - |  |
| `UFN.04` | Om API:et stödjer begränsning av returnerade fält, komplicerad filtrering, sortering eller paginering BÖR API:et stödja:<br>• fields - specificera eller begränsa vilka fält som ska returneras<br>• filter - villkor som begränsar/filtrerar resultatets träffbild vid mer komplicerad filtrering (t.ex. större än). Vid enkel filtrering använd exempelvis ”=” direkt<br>• sort - hur resultatet ska sorteras<br>• page - specificerar vilken sida som ska returneras i en träffbild | **BÖR** | - |  |
| **URL maxlängd** | | | | |
| `UFN.05` | En URL BÖR INTE vara längre än 2048 tecken. | **BÖR INTE** | - |  |
| **URL namnsättningskonvention** | | | | |
| `UFN.07` | URL:n SKALL använda dessa tecken (a-z, 0-9, "-", "." samt "~", se vidare i RFC 3986) (gäller URL-elementen Scheme, Authority och Path samt API-elementen protokoll, domännamn, api, version, resurs och identifierare). | **SKALL** | - |  |
| `UFN.08` | Endast bindestreck '-' SKALL användas för att separera ord för att öka läsbarheten samt förenkla för sökmotorer att indexera varje ord för sig (gäller URL-elementen Authority och Path. Mer specifikt API-elementen domännamn, api, resurs, identifierare). | **SKALL** | - |  |
| `UFN.09` | Blanksteg ' ' och understreck '_' SKALL INTE användas i URL:er med undantag av query parameter-delen (gäller alltså URL-elementen Scheme, Authority och Path samt API-elementen protokoll, domännamn, api, version, resurs och identifierare). | **SKALL INTE** | - |  |

## [Mognad](https://dev.dataportal.se/rest-api-profil/mognad)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `MOG.01` | Alla API:er SKALL designas för att uppnå nivå 2 enligt Richardson Maturity Model. | **SKALL** | - |  |
| `MOG.02` | Alla API:er BÖR samtidigt designas för att uppnå nivå 3 enligt Richardson Maturity Model. | **BÖR** | - |  |
| `MOG.03` | Om man använder POST operationerna där man vanligen använder GET operationer SKALL det dokumenteras tydligt varför. | **SKALL** | - |  |

## [Säkerhet](https://dev.dataportal.se/rest-api-profil/sakerhet)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| **Transportsäkerhet** | | | | |
| `SAK.01` | All transport SKALL sker över HTTPS med minst TLS 1.2. | **SKALL** | - |  |
| `SAK.02` | Alla certifikat SKALL vara från SHA-2 (Secure Hash Algorithm 2) kryptografiska hashfunktioner med minsta nyckellängd på 2048. | **SKALL** | - |  |
| `SAK.03` | Alla offentligt tillgängliga endpoints SKALL använda ett digitalt certifikat som har undertecknats av en godkänd certifikatutfärdare. | **SKALL** | - |  |
| `SAK.05` | Oanvända HTTP-metoder BÖR inaktiveras och returnera HTTP-status 405. | **BÖR** | - |  |
| `SAK.06` | Alla requests SKALL valideras. | **SKALL** | - |  |
| `SAK.08` | Om man i informationsklassningen har restriktioner till vilka som får konsumera informationen i API:et så SKALL autentisering och auktorisation användas. | **SKALL** | - |  |
| **Autentisering och auktorisation** | | | | |
| `SAK.09` | Basic- eller Digest-autentisering SKALL INTE användas. | **SKALL INTE** | - |  |
| `SAK.10` | Authorization: Bearer header SKALL användas för autentisering/auktorisation. | **SKALL** | - |  |
| `SAK.11` | En uppdateringstoken SKALL tillhandahållas för att förlänga giltighetstiden för befintliga token utan att behöva tillhandahålla referenserna igen. | **SKALL** | - |  |
| `SAK.12` | Ställ alltid in en rimlig giltighetstid för tokens. En OIDC-åtkomsttokens livslängd BÖR INTE överstiga 5 minuter. | **BÖR INTE** | - |  |
| `SAK.13` | Klientapplikationsidentitet BÖR etableras via en konsekvent mekanism. Detta kan vara via en API-nyckel eller via en mer robust mekanism som en OAuth-serveridentitet. | **BÖR** | - |  |
| `SAK.14` | Rotationspolicy för API-nycklar BÖR implementeras där så är tillämpligt. | **BÖR** | - |  |
| `SAK.15` | API-nycklar SKALL INTE inkluderas i URL eller querysträngen. | **SKALL INTE** | - |  |
| `SAK.16` | API-nycklar SKALL inkluderas i HTTP-headern eftersom querysträngar kan sparas av klienten eller servern i okrypterat format av webbläsaren eller serverapplikationen. | **SKALL** | - |  |
| `SAK.17` | CORS-headers (Cross-origin resource sharing) BÖR endast användas när det är nödvändigt eftersom det kringgår de övergripande säkerhetsmekanismerna som är inbyggda i webbläsare genom att selektivt lätta på restriktioner för cross-origins, RFC 6454. | **BÖR** | - |  |
| **Oauth** | | | | |
| `SAK.18` | OAuth version 2.0 eller senare BÖR användas för auktorisation. | **BÖR** | - |  |
| **Abstraktion** | | | | |
| `SAK.19` | Som en allmän princip BÖR det finnas en abstraktionsnivå av källsystemets data- och affärslogik i din API-design för att frikoppla konsumenterna och API-tjänsteleverantörerna. | **BÖR** | - |  |
| `SAK.20` | • Mellan - API:er som syftar till återanvändning BÖR överväga mellanliggande abstraktionsnivån. | **BÖR** | - |  |
| `SAK.21` | • Mellan - Interna databastabellstrukturer SKALL INTE exponeras direkt i ditt API. | **SKALL INTE** | - |  |
| `SAK.22` | • Mellan - Exponering av interna systemidentifierare BÖR INTE (exempelvis ett databas-ID) exponeras till konsumenter. | **BÖR INTE** | - |  |
| `SAK.23` | • Avancerad - HATEOAS BÖR användas för att abstrahera tillåtna åtgärder. | **BÖR** | - |  |
| **Felhantering** | | | | |
| `SAK.24` | Felmeddelanden SKALL INTE avslöja information som kan användas för att attackera ditt system. | **SKALL INTE** | - |  |
| `SAK.25` | Ett API SKALL maskera alla systemrelaterade fel bakom vanliga HTTP-statuskoder och felmeddelanden, t.ex. exponera inte information på systemnivå i ditt felmeddelande. | **SKALL** | - |  |
| `SAK.26` | Ett API SKALL INTE skicka tekniska detaljer (t.ex. anropsstackar eller andra interna tips) till klienten. | **SKALL INTE** | - |  |
| **Loggning** | | | | |
| `SAK.27` | En viktig aspekt av säkerheten är att bli meddelad när något fel inträffar och att kunna undersöka det. Man BÖR definiera loggningsnivåer som kan användas för att trigga lämpliga varningar och larm. | **BÖR** | - |  |
| **Validering av ingående parametrar** | | | | |
| `SAK.28` | Validering av ingående parametrar BÖR utföras för att säkerställa att endast korrekt formaterad data tas emot av ditt system, detta hjälper till att förhindra skadliga attacker. | **BÖR** | - |  |
| **Validering av innehållstyp** | | | | |
| `SAK.29` | Man BÖR respektera angiven content-type i header. | **BÖR** | - |  |
| **Gateway säkerhetsfunktioner** | | | | |
| `SAK.30` | HTTP verb BÖR användas för att beskriva sökvägen för ett API. | **BÖR** | - |  |
| `SAK.31` | Olika typer av filter BÖR användas bekräfta giltigheten på input till ett API. Exempelvis meddelandestorlek, kontroll av scheman, kontroll av headers eller kontroll av sökparametrar. | **BÖR** | - |  |
| `SAK.32` | SSL protokoll (TLS 1.2) BÖR användas. | **BÖR** | - |  |
| `SAK.35` | API Keys BÖR användas för identifiera en klient. | **BÖR** | - |  |
| `SAK.36` | OAUTH BÖR användas för identifiera en klient. | **BÖR** | - |  |
| `SAK.37` | CORS BÖR tillåta resursdelning över domängränser när det är nödvändigt. | **BÖR** | - |  |

## [API Message](https://dev.dataportal.se/rest-api-profil/api-message)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `AME.01` | Datamodellen för en representation BÖR beskrivas med JSON enligt senaste versionen, RFC 8259. | **BÖR** | - |  |
| **Datamodell för representation** | | | | |
| `AME.02` | Det BÖR förutsättas att alla request headers som standard använder Accept med värde application/json. | **BÖR** | - |  |
| **Namnsättningskonvention för message** | | | | |
| `AME.04` | För fältnamn i request och response body BÖR camelCase eller snake_case notation användas. | **BÖR** | - |  |
| `AME.05` | Inom ett API SKALL namnsättningen vara konsekvent, dvs blanda inte camelCase och snake_case. | **SKALL** | - |  |
| `AME.06` | Fältnamn som representerar listor SKALL namnges i pluralform. | **SKALL** | - |  |
| `AME.07` | Fältnamn BÖR använda tecken som är alfanumeriska. | **BÖR** | - |  |

## [API Request](https://dev.dataportal.se/rest-api-profil/api-request)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `ARQ.01` | Ett API request BÖR skickas i UTF-8 format. | **BÖR** | - |  |
| **Request headers** | | | | |
| `ARQ.02` | Alla API:er SKALL supportera följande request headers:<br>• Authorization<br>• Content-type | **SKALL** | - |  |
| `ARQ.03` | Alla API:er BÖR supportera följande request headers:<br>• Accept = application/json<br>• Date<br>• Cache-Control<br>• If-None-Match<br>• Connection<br>• Cookie | **BÖR** | - |  |
| `ARQ.05` | Payload data SKALL INTE användas i HTTP-headers. | **SKALL INTE** | - |  |
| **Idempotens** | | | | |
| `ARQ.06` | Följande REST API-metoder SKALL vara idempotenta:<br>• GET<br>• PUT<br>• DELETE<br>• HEAD<br>• OPTION | **SKALL** | - |  |

## [API Response](https://dev.dataportal.se/rest-api-profil/api-response)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| **HTTP Response koder** | | | | |
| `ARP.02` | Svar som är vanligt förekommande BÖR stödjas av ett API när det är tillämpbart enligt RFC 9110. | **BÖR** | - |  |
| `ARP.03` | HTTP 201 svarets HTTP header Location BÖR returneras för att ange var den nyskapade resursen är tillgänglig. | **BÖR** | - |  |
| `ARP.04` | HTTP 202 svarets HTTP header Location BÖR returneras för att ange var den skapade resursen kommer att vara tillgänglig. | **BÖR** | - |  |

## [Felhantering](https://dev.dataportal.se/rest-api-profil/felhantering)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `FEL.01` | Om HTTP svarskoderna inte räcker SKALL API:et beskriva feldetaljer enligt schemat nedan.<br>Schemat enligt RFC 9457 kan innehålla nedan attribut och kräver att mediatypen application/problem+json eller application/problem+xml används i svaret.<br>• type<br>• title<br>• status<br>• detail<br>• instance | **SKALL** | - |  |
| `FEL.02` | Schemat enligt RFC 9457 BÖR innehålla de beskrivna attributen i FEL.01 och SKALL (FEL.02) använda mediatypen application/problem+json eller application/problem+xml i svaret. | **BÖR** | - |  |

## [Versionshantering](https://dev.dataportal.se/rest-api-profil/versionhantering)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| **Bakåtkompatibilitet** | | | | |
| `VER.01` | Ett API BÖR så långt som möjligt ha en lös koppling mellan producent och konsument. | **BÖR** | - |  |
| `VER.02` | Som producent av ett API BÖR man undvika att införa icke bakåtkompatibla ändringar i sitt API och ibland är det oundvikligt. | **BÖR** | - |  |
| `VER.03` | Som konsument BÖR man alltid vara tolerant, förvänta sig och hantera oväntade svar i ett meddelande. | **BÖR** | - |  |
| **Versionsnumrering** | | | | |
| `VER.04` | Samtliga API:er SKALL använda semantisk versionshantering. | **SKALL** | - |  |
| `VER.05` | Version BÖR  anges i URL enligt formatet v[x] där 'v' avser förkortning för version och x avser ett och bara ett nummer (0-n) för major-version | **BÖR** | - |  |
| **Information om ett API och dess version** | | | | |
| `VER.06` | Information om ett API SKALL tillgängliggöras via resursen api-info under roten till API:et. | **SKALL** | - |  |
| `VER.07` | Genom att utföra GET på resursen api-info SKALL följande information returneras:<br>• apiName: Namn på API<br>• apiVersion: API-version med MAJOR, MINOR och PATCH version<br>• apiReleased: Datum som denna API-version publicerades<br>• apiDocumentation: En länk till aktuell API dokumentation. Om API:et  består av flera delar (t.ex. flera webbsidor) bör en samlingsplats skapas för  alla delar skapas där alla delar är åtkomliga via länkar eller liknande<br>• apiStatus: Visar API:ets tillstånd eller status enligt livscykelhantering  längre ned på denna sida. T.ex. beta, active eller deprecated | **SKALL** | - |  |
| **API utfasning** | | | | |
| `VER.08` | API ägare BÖR informera konsumenten i svaret på ett anrop, när en gammal version används. | **BÖR** | - |  |
| `VER.09` | Fältet Deprecation följt av ett datum i svaret indikerar för konsumenten att en nyare version av API:t finns. Fältet Sunset följt av ett datum i svaret indikerar att denna version av API:t slutar att fungera vid det aktuella datumet. Datumet anges enligt standarden RFC 3339. Till dessa två fält kan man också lägga till länkar till den nyaste versionen eller ett alternativt API som man kan använda. Något av följande relationsvärden BÖR användas:<br>• successor-version: Pekar på den resurs som efterträdande den resurs som anropades (RFC 5829)<br>• latest-version: Pekar på den senaste versionen av resursen (RFC 5829)<br>• alternate: Pekar på en alternativ resurs (HTML 4.01 Specification) | **BÖR** | - |  |
| **Tillstånd för ett API** | | | | |
| `VER.10` | Följande tillstånd eller status BÖR användas för att beskriva vart i livscykeln ett API befinner sig i:<br>• alpha<br>• beta<br>• active<br>• deprecated<br>• retired<br>• decommisioned | **BÖR** | - |  |
| **Versionshanteringsregler för ett API:s livscykel** | | | | |
| `VER.11` | Versioner som befinner sig i status alpha eller beta SKALL börja med en MAJOR version med siffran ”0”. | **SKALL** | - |  |
| `VER.12` | API:ets första publika version i status active SKALL alltid börja med en MAJOR version med siffran ”1”. | **SKALL** | - |  |
| `VER.13` | Eftersom MINOR versioner är bakåtkompatibla med föregående MINOR versioner inom samma MAJOR version SKALL MINOR versioner anses retired direkt när en ny MINOR version av samma MAJOR version blir active. | **SKALL** | - |  |
| `VER.14` | När en ny MAJOR version publiceras så SKALL de äldre versionerna fasas ut. | **SKALL** | - |  |
| `VER.15` | En MAJOR version SKALL INTE sättas till deprecated förrän en ersättare är active och det finns en tydlig migrationsväg för all funktionalitet som upprätthålls framåt. | **SKALL INTE** | - |  |
| `VER.16` | Detta BÖR inkludera dokumentation och migrationsverktyg/exempelkod som beskriver vad konsumenterna behöver göra för en ren migration. | **BÖR** | - |  |
| `VER.17` | Om ett API ska sättas till deprecated och samtidigt har externa konsumenter BÖR tid för utfasning och begränsningar övervägas för att minimera kundpåverkan. | **BÖR** | - |  |
| `VER.18` | API i statusen deprecated SKALL vara i status deprecated under en tillräckligt lång tid för att ge konsumenterna möjlighet att kunna migrera. | **SKALL** | - |  |
| `VER.19` | Om ett API i status active eller deprecated inte har några användare så BÖR det direkt sättas i status retired. | **BÖR** | - |  |

## [Spårbarhet och korrelation](https://dev.dataportal.se/rest-api-profil/sparbarhet-och-korrelation)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `SPA.01` | API-producenter SKALL stödja spårning av API-anrop enligt W3C Trace Context | **SKALL** | - |  |
| `SPA.02` | API-producenter SKALL (SPA.02) acceptera HTTP-headern traceparent i inkommande anrop och propagera spårningsinformationen vidare enligt W3C Trace Context vid vidare anrop till andra system. | **SKALL** | - |  |
| `SPA.03` | Om traceparent saknas i ett inkommande anrop SKALL API-producenten generera en ny traceparent enligt W3C Trace Context. | **SKALL** | - |  |
| `SPA.04` | API-producenten BÖR inkludera HTTP-headern traceparent i ett API-svar. | **BÖR** | - |  |
| `SPA.06` | Spårningsinformation som används vid API-anrop BÖR användas för att möjliggöra korrelation mot applikationsloggar och revisionsloggar. | **BÖR** | - |  |
| `SPA.07` | Alternativa identifierare, såsom x-request-id, kan användas som komplement för interna behov, men SKALL INTE ersätta traceparent vid spårning av API-anrop mellan system och organisationer. | **SKALL INTE** | - |  |

## [Webhooks](https://dev.dataportal.se/rest-api-profil/webhooks)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `WEB.01` | Om Webhooks väljs att implementeras så bör följande designriktlinjer övervägas:<br>• Om känslig information skickas i Webhook, SKALL kryptering/signering övervägas i POST request för att erhålla integritet och verifikation. TLS certifikat är också ett alternativ. | **SKALL** | - |  |
| `WEB.02` | Om Webhooks väljs att implementeras så bör följande designriktlinjer övervägas:<br>• Ett unikt händelseid/transaktionsid BÖR inkluderas i POST request, för att underlätta felsökning och loggning. | **BÖR** | - |  |
| `WEB.03` | Om Webhooks väljs att implementeras så bör följande designriktlinjer övervägas:<br>• Konsumentens endpoint SKALL alltid returnera ett svar på inkommen POST, så producenten vet att svaret är levererat. | **SKALL** | - |  |
| `WEB.04` | Om Webhooks väljs att implementeras så bör följande designriktlinjer övervägas:<br>• API SKALL stödja en återsändningsfunktion i de fall konsumentens URL inte är tillgänglig, eller möjliggöra att konsument gör en request som använder tidpunkt eller räknare. | **SKALL** | - |  |
| `WEB.05` | Om Webhooks väljs att implementeras så bör följande designriktlinjer övervägas:<br>• En tydlig onboarding/offboarding process SKALL finnas, som beskriver den process som konsumenten genomgår för att börja använda webhooks mot producenten samt hur avslut av samma sker. | **SKALL** | - |  |
| `WEB.06` | Om Webhooks väljs att implementeras så bör följande designriktlinjer övervägas:<br>• Eftersom användandet av Webhooks kräver ytterligare infrastruktur hos API konsumenterna, BÖR detta tas hänsyn till när lösningen designas. | **BÖR** | - |  |

## [Hypermedia](https://dev.dataportal.se/rest-api-profil/hypermedia)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `HYP.01` | Ett API SKALL ge god dokumentation som tydligt beskriver alla länkar, dess relationstyper och de svarsformat som varje URL ger. | **SKALL** | - |  |
| **Hypermedia kompatibla API:er** | | | | |
| `HYP.02` | En GET-förfrågan SKALL INTE ändra tillstånd på den resurs som hämtas. | **SKALL INTE** | - |  |
| `HYP.03` | För att kunna erbjuda en bättre upplevelse för API konsumenterna, BÖR API:er erbjuda en lista på tillståndsförändringar som finns tillgängliga för varje resurs i  _links listan. | **BÖR** | - |  |
| `HYP.04` | Ett svar BÖR returnera relaterade och giltiga länkelement. | **BÖR** | - |  |
| `HYP.05` | Ett svar SKALL också returnera ett länkelement till sig själv, self. | **SKALL** | - |  |
| **Hypermedia response** | | | | |
| `HYP.06` | Existerande standarder, såsom JSON, ATOM, Collection-JSON, HAL m.fl. BÖR användas istället för egna format. | **BÖR** | - |  |
| **Relativa kontra absoluta URL:er** | | | | |
| `HYP.07` | Webbadresserna som genereras av API:et BÖR vara absoluta webbadresser. | **BÖR** | - |  |
| **API Gateways** | | | | |
| `HYP.08` | Som regel SKALL API Management gateway skicka ytterligare en header med i anropet till backend API:et som innehåller API Gateway bas URL:en för det aktuella API:et. | **SKALL** | - |  |
| `HYP.09` | Headern SKALL namnges som forwarded, enligt beskrivningen i RFC 7239. | **SKALL** | - |  |
| `HYP.10` | Om forward header inte finns BÖR API backend använda sin egen absoluta bas URL som fallback. | **BÖR** | - |  |
| **Link Description Object** | | | | |
| `HYP.11` | Länkar SKALL beskrivas med ett Link Description Object schema. | **SKALL** | - |  |
| `HYP.12` | Ett värde för href SKALL ges. | **SKALL** | - |  |
| `HYP.13` | Värdet för href SKALL vara en URL som används för att bestämma målet för den relaterade resursen. | **SKALL** | - |  |
| `HYP.14` | Används endast absoluta URL värden för href. Klienter kan spara URL för en länk för att använda senare. Utvecklare SKALL använda värden från inkommande host headern i den absoluta URL:n. | **SKALL** | - |  |
| `HYP.15` | Ett värde för rel SKALL anges. | **SKALL** | - |  |
| `HYP.16` | method identifierar HTTP verbet som SKALL användas i förfrågan i länken. | **SKALL** | - |  |
| `HYP.17` | Om ingen method anges är GET defaultmetod, men method SKALL anges. | **SKALL** | - |  |
| **Link Relation Type** | | | | |
| `HYP.18` | Ett API SKALL tilldela en meningsfull Link Relation Type som entydigt beskriver semantiken för länken. | **SKALL** | - |  |
| `HYP.19` | När semantiken av en Link Relation Type som önskas användas också återfinns i IANA's lista över standardiserade länkrelationer SKALL IANA’s typ användas. | **SKALL** | - |  |

## [Filtrering, namnsättning och sökparametrar](https://dev.dataportal.se/rest-api-profil/filtrering-paginering-och-sokparametrar)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| **Namnsättningskonvention för sökparametrar** | | | | |
| `FNS.01` | Parameternamn SKALL anges med en konsekvent namnkonvention inom ett API, exempelvis antingen snake_case eller camelCase. | **SKALL** | - |  |
| `FNS.02` | Sökparametrars värden SKALL vara percent-endcoded (RFC 3986) på UTF-8 format. | **SKALL** | - |  |
| `FNS.03` | Sökparametrar SKALL starta med en bokstav. | **SKALL** | - |  |
| `FNS.05` | Sökparametrar BÖR vara frivilliga. | **BÖR** | - |  |
| `FNS.06` | Sökparametrar BÖR använda tecken som är URL-säkra (tecknen A-Z, a-z, 0-9, "-", ".", "_" samt "~", se vidare i RFC 3986). | **BÖR** | - |  |
| **Paginering** | | | | |
| `FNS.07` | Vid användande av paginering, SKALL följande parametrar ingå i request:<br>• page eller offset<br>• limit | **SKALL** | - |  |
| `FNS.08` | page SKALL alltid starta med värde '1'. | **SKALL** | - |  |
| `FNS.09` | Defaultvärde för limit BÖR vara '20'. | **BÖR** | - |  |
| `FNS.10` | När paginering implementeras, BÖR man inkludera fält för _meta och _links för att förse konsumenten med information om resultatet, och erbjuda enkel navigation i träffbilden. | **BÖR** | - |  |
| `FNS.11` | _links fältet BÖR inkludera länkar for self, first, last, next och prev. | **BÖR** | - |  |
| `FNS.12` | Varje länk SKALL inkludera alla eventuellt övriga frågeparametrar som skickades i första request. | **SKALL** | - |  |
| `FNS.13` | Följande struktur BÖR appliceras på meta objektet:<br>• total records<br>• page<br>• offset<br>• limit<br>• count | **BÖR** | - |  |
| **Utanför träffbilden** | | | | |
| `FNS.14` | Om konsumenten begär en sida som är utanför det giltiga spannet för träffbilden, tex om sidspannet går från 1 till 15 men konsumenten begär ?page=0 eller ?page=99, BÖR svaret innehålla en tom träffbild med HTTP statuskod 200. | **BÖR** | - |  |

## [Cachning](https://dev.dataportal.se/rest-api-profil/cachning)

| ID | Krav | Typ | Status | Kommentar |
| :--- | :--- | :--- | :---: | :--- |
| `CAC.01` | GET-request BÖR kunna cachelagras som standard. | **BÖR** | - |  |