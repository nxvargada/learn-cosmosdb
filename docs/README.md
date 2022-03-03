# Azure CosmosDB

## Mi a NOSql?

## SQL vs NOSQL

## Azure Cosmos DB tulajdonságai

Skálázható NOSql adatbázis platform, ami a felhőben fut.
Azure PaaS (Platform as a service)
Horizontális partícionálás

## [Létrehozás Azure-ben](https://portal.azure.com/#@nexius.hu/resource/subscriptions/8a2dc724-05fa-4f5c-93ee-fa7cdc3602ac/resourcegroups/rg-njord-shared/providers/Microsoft.DocumentDB/databaseAccounts/cosmos-introduction-dev/overview)

## Név konvenciók

`cosmos-{app-name}-{environment}`

Megjegyzés: [Dokumentáció](https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming#example-names-databases)

## Adatbázis hozzáférése

Kulcsokkal illetve connection string-ekkel férünk hozzá az Accounthoz. Két fajta kulcs létezik

- `Read-write keys`
- `Read-only keys`

## Data Explorer

## Konténer létrehozása

- Full screen mode
- Database ID
- Container ID
- Partition key
- Unique key

Azure CLI, .NET SDK-val is tudunk létrehozni container-t

## Dokumentum létrehozása

Partition key: ez egy property, ha különbüzik a partition key a dokumentumoknak, akkor különböző logikai partíciókon vannak. Immutable, nem lehet sem a container-re sem a document-re megváltoztatni!
Cross Partition query OR Single partition query ha a query-ben nem adjuk meg a partionkey-t akkor
akkor cross partition query lesz belőle, mert az összes clusteren végig fog menni, mert nem tudja,
hogy pontosan melyik logikai partícióban keresse. Késztítsünk egy ábrát ami szemlélteti a container,
fizikai és logikai partíciót. Nagyon nem mindegy hogy milyen partition key-t választunk.
(Choosing the right partition key)
Pár példa query-k:
Where IS_DEFINED(c.pets)
WHERE ARRAY_LENGTH(c.kids) > 2

## Latency és Throughput

Latency: milyen gyorsan ad választ (response) egy kérésre (request)?
Throughput: Hány kérést tud kiszolgálni adott időn belül?

Request Units (RU): nem a request-et jelenti. Letöltött kép!!!! Egyik request sem egyforma. Ugyanaza a request
mindig ugyanannyi RU-t fog felhasználni (Determinisztikus). RU alapján fizetünk. Nézzünk erre egy query metric-et
Data Explorer-ben.

Complexebb query-nél magasabb RU:

```SQL
SELECT * FROM c
Where c.address.city = 'Chicago' AND
ARRAY_LENGTH(c.kids) > 2 AND
STARTSWITH(c.address.addressLine, '123')
```

[Metrikák](https://portal.azure.com/#@nexius.hu/resource/subscriptions/8a2dc724-05fa-4f5c-93ee-fa7cdc3602ac/resourcegroups/rg-njord-shared/providers/Microsoft.DocumentDB/databaseAccounts/cosmos-introduction-dev/metricsclassic)

## Cost management

1. Manual (Provisioned throughput)
2. Autoscale (Provisioned throughput)
3. Serverless

Ha a container nem tudja kiszolgálni már a kéréseket, mert pl. meghaladtuk a 400 RU/s limit-et,
akkor HTTP 429 Too many requests kódot kapunk. (Request throttle). Ilyenkor a Header-be belekerül egy
Retry-After információ, hogy mennyi idő elteltével próbálja meg újra kérést indítani. Ha ezekután sem sikerül,
akkor exception-t kapunk. (.NET SDK-án keresztül használtuk). Ekkor az egyik megoldás, ha az RU számát
felemeljük vagy autoscale.

Másik megoldás, ha a container létrehozásakor `Share throughput across containers` opciót választjuk,
Ekkor adatbázis szinten kezeljük, ami azt jelenti ha például van 4 container-ünk alap 400 RU/s
max limit-tel, akkor ha érkezik egy nagyobb terhelés az egyik kontainer-re, akkor a többi konténer szabad
RU/s-et átadja neki. Ezzel akár a négyszerésre is nőhet a request-ek kiszolgálása. Költségben nem lesz magasabb.
Amíg a konténrek meg tudják osztani a teljesítményt, addig nem kell skálázni. (Autoscale drágább)
Tervezést igényel, hogy hány és milyen container-erekre osszuk fel az adatbázist.

## Horizontal Partitioning

Fizikai partíció (1 fizikai partíció 1 computer) 1 container adatai több fizikai gépen is
tárolódhatnak, ezért van unlimited storage és throughput
Logikai partíció (Partition key)

## Global Distribution

Read és Write Regio. Ha az alkalmazás nem egy régióban van a CosmosDb-vel, akkor a latency megnő.
Replicate Data globally: lehetőségünk van több regiot használni ezzel is növelni a latency-t.
.Net SDK-n keresztül át tudjuk adni, hogy melyik Regiót használja az alkalmazás, és ez alapján fogja
a Cosmos db-t használni.

### Multiple Region Conflict

Conflict resolution

1. Last Write Wins
2. Custom Stored Procedure own logic

Conflicts feed: lehetőség van a update és törölni is.

## Data Modelling

- JSON document
- Non-relational
- No JOINs
- No relational constraints

| Relational database    | Document database |
| ---------------------- | ----------------- |
| Row                    | Document          |
| Columns                | Properties        |
| Strongly typed schemas | No defined schema |

Document 1

```json
{
	"name": "Robert Pattinson",
	"job": "actor",
	"type": "user",
	"version": "1"
}
```

Document 2

```json
{
	"fullName": "Robert Pattinson",
	"job": "actor",
	"type": "user",
	"version": "2"
}
```

Document 3

```json
{
	"fullName": {
		"firstName": "Robert",
		"lastName": "Pattinson"
	},
	"job": "actor",
	"type": "user",
	"version": "3"
}
```

### One to many relationship

Document:

```json
{
	"userId": "1",
	"fullName": {
		"firstName": "Robert",
		"lastName": "Pattinson"
	},
	"job": "actor",
	"type": "user",
	"movies": [
		{
			"id": "1",
			"tite": "Twilight",
			"year": "2008",
			"imdb": "5.3"
		},
		{
			"id": "2",
			"title": "The Batman",
			"year": "2022",
			"imdb": "9.1"
		}
	],
	"version": "3"
}
```

Egy dokumentum 2MB méretű lehet. Mi van akkor, ha meghaladja ezt a méretet?
Tegyük fel, hogy a nem kettő film van, hanem 500, és meghaladja a 2MB-ot, akkor pl. 100-ával
szétosztjuk a documentumot, de csak a filmeknél.

```json
{
    "userId": "1",
    "fullName": {
        "firstName": "Robert",
        "lastName": "Pattinson"
    },
    "job": "actor",
    "type": "user",
    "movies": [
        {
            "id": "1",
            "tite": "Twilight",
            "year": "2008",
            "imdb": "5.3"
        },
        {
            "id": "2",
            "title": "The Batman",
            "year": "2022",
            "imdb": "9.1"
        }
        {

        },...
        {
            "id": "100"
        }
    ],
    "version": "3"
}
```

```json
{
    "userId": "1",
    "movies": [
        { "id": 101 },
        ...,
        { "id": 200 }
    ]
}
```

### Special Document Properties

| Property      | Value                         |
| ------------- | ----------------------------- |
| id            | User-defined unique ID        |
| user-defined  | Partition key                 |
| \_rid         | Resource ID                   |
| \_self        | URI path to the resource      |
| \_etag        | GUID (optimistic concurrency) |
| \_attachments | URI suffix to the attachents  |
| \_ts          | Last updated timestamp        |
| ttl           | Time to Live (expiration)     |

## SDKs

Supported languages

- .NET / .NET Core
- Java
- Node.js
- Python
