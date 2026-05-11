# LB223 – Lagerbestandssystem

> Multi-User-Applikation zur Verwaltung von Lagerbeständen. Mehrere Benutzer können gleichzeitig Artikel ein- und ausbuchen, wobei der Bestand durch Transaktionen und pessimistisches Locking jederzeit konsistent bleibt.

---

## Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [Architektur](#architektur)
- [Datenmodell (ERD)](#datenmodell-erd)
- [API-Dokumentation](#api-dokumentation)
- [Wichtige Abläufe](#wichtige-abläufe)
- [Voraussetzungen](#voraussetzungen)
- [Installation & Start](#installation--start)
- [Testbenutzer](#testbenutzer)
- [Tests ausführen](#tests-ausführen)

---

## Übersicht

Das Lagerbestandssystem ermöglicht es mehreren gleichzeitigen Benutzern, Artikel einzusehen und deren Bestand zu buchen. Nach dem Login sieht jeder Benutzer die aktuelle Bestandsliste und kann pro Artikel eine Einbuchung (Bestand erhöhen) oder Ausbuchung (Bestand verringern) auslösen. Das System verhindert zuverlässig, dass der Bestand unter 0 fällt – auch wenn mehrere Benutzer exakt zur gleichen Zeit buchen (Lost-Update-Problem gelöst durch Serializable-Transaktion und UPDLOCK/ROWLOCK).

---

## Architektur

```mermaid
graph TD
    FE["Angular 21\nFrontend\n:4200"]
    API["ASP.NET Core 10\nREST API\n:5000"]
    DB["SQL Server\n(Docker)\n:1433"]

    FE -->|"HTTP /api (Proxy)"| API
    API -->|"EF Core"| DB

    subgraph Backend ["Backend (3 Schichten)"]
        PL["Presentation Layer\nControllers"]
        BL["Business / Domain Layer\nEntities, Interfaces, Enums"]
        DAL["Data Access Layer\nRepositories, DbContext"]
        PL --> BL
        BL --> DAL
    end

    API --- PL
```

---

## Datenmodell (ERD)

```mermaid
erDiagram
    Article {
        int Id PK
        string Name
        int Quantity
        byte[] RowVersion
    }
    User {
        int Id PK
        string Username
        string PasswordHash
    }
```

`RowVersion` ist ein EF Core Concurrency Token, das automatisch bei jeder Änderung neu gesetzt wird.

---

## API-Dokumentation

Die vollständige interaktive Dokumentation ist über Swagger verfügbar: **`http://localhost:5000/swagger`**

| Methode | Endpoint | Auth | Beschreibung |
|---------|----------|------|--------------|
| POST | `/api/auth/login` | – | Login, gibt JWT zurück |
| GET | `/api/articles` | Bearer | Alle Artikel mit aktuellem Bestand |
| POST | `/api/articles/{id}/book` | Bearer | Artikel ein- oder ausbuchen |

**BookingRequest Body:**
```json
{
  "type": "Einbuchen",
  "amount": 5
}
```
`type` ist entweder `"Einbuchen"` oder `"Ausbuchen"`.

---

## Wichtige Abläufe

### Login

```mermaid
sequenceDiagram
    participant FE as Angular Frontend
    participant API as ASP.NET Core API
    participant DB as SQL Server

    FE->>API: POST /api/auth/login {username, password}
    API->>DB: SELECT User WHERE Username = ?
    DB-->>API: User (PasswordHash)
    API-->>API: BCrypt.Verify(password, hash)
    API-->>FE: 200 OK {token}
    FE-->>FE: Token in localStorage speichern
```

### Ausbuchen (Mehrbenutzer-Szenario)

```mermaid
sequenceDiagram
    participant A as Benutzer A
    participant API as API
    participant DB as SQL Server
    participant B as Benutzer B

    A->>API: POST /api/articles/1/book {Ausbuchen, 1}
    B->>API: POST /api/articles/1/book {Ausbuchen, 1}
    API->>DB: BEGIN TRANSACTION (Serializable)
    API->>DB: SELECT * FROM Articles WITH (UPDLOCK, ROWLOCK) WHERE Id = 1
    Note over DB: Sperre gesetzt – Benutzer B wartet
    API-->>API: Validierung: Quantity >= 1 ✓
    API->>DB: UPDATE Quantity = 0, COMMIT
    Note over DB: Sperre freigegeben
    DB-->>API: Benutzer B erhält Quantity = 0
    API-->>API: Validierung: 0 >= 1 ✗
    API-->>B: 409 Conflict "Nicht genug Bestand. Verfügbar: 0"
    API-->>A: 200 OK
```

---

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 24](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Installation & Start

### 1. Repository klonen

```bash
git clone [Repository-URL]
cd lb223
```

### 2. Datenbank starten

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=LB223_Dev!Pass" \
  -p 1433:1433 --name lb223-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

### 3. Backend starten

```bash
cd backend/Api
dotnet run
```

Die API ist erreichbar unter: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

Migrationen und Seed-Daten (Artikel + Testbenutzer) werden beim Start automatisch angewendet.

### 4. Frontend starten

```bash
cd frontend/frontend
npm install
npm start
```

Das Frontend ist erreichbar unter: `http://localhost:4200`

---

## Testbenutzer

Die folgenden Benutzer werden beim ersten Start automatisch angelegt:

| Benutzername | Passwort |
|-------------|----------|
| `admin` | `Admin123` |
| `user1` | `User123` |
| `user2` | `User123` |

---

## Tests ausführen

Die Tests befinden sich in `backend/Tests/` und laufen gegen eine echte SQL Server Instanz (Port 1433 muss erreichbar sein).

```bash
cd backend
dotnet test
```

| Testklasse | Test | Beschreibung |
|---|---|---|
| `BookingUnitTests` | `ParallelAusbuchen_NurEineErfolgreich` | 2 parallele Ausbuchungen bei Bestand 1 → exakt 1 Erfolg |
| `BookingUnitTests` | `ParallelEinbuchen_AlleErfolgreich` | 5 parallele Einbuchungen → alle erfolgreich, Bestand = 5 |
| `LoadTests` | `LastTest_Ausbuchen_BestandNieUnterNull` | 10 parallele Ausbuchungen bei Bestand 5 → Bestand nie < 0 |
| `LoadTests` | `LastTest_Einbuchen_AlleErfolgreich` | 20 parallele Einbuchungen → alle erfolgreich, Bestand = 20 |

---

_Erstellt im Rahmen von Modul 223 – Laurin Monnerat – Mai 2026_
