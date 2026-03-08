# Database Schema

SQLite database managed via sqlite-net-pcl. Foreign key relationships are enforced in the Repository layer (C# code), not via SQL constraints.

## Entity-Relationship Diagram

```
  StringSets (1)
       |
       |--- SetId (FK)
       v
  StringBaselines (6 per set)
       |
       |--- BaselineId (FK)
       v
  MeasurementLogs (*)


  Guitars (1)
       |
       |--- GuitarId (FK)
       v
  GuitarStringSetPairings (*) ---< SetId (FK) ---> StringSets (1)
```

## Tables

### StringSets

Stores guitar string set metadata (brand, model, individual gauges per string).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Unique identifier |
| Brand | TEXT | NOT NULL | Manufacturer (e.g., "Ernie Ball", "D'Addario") |
| Model | TEXT | NOT NULL | Product line and gauge range (e.g., "Regular Slinky (10-46)") |
| GaugeE1 | REAL | DEFAULT 0 | High E (1st string) gauge in inches |
| GaugeB2 | REAL | DEFAULT 0 | B (2nd string) gauge |
| GaugeG3 | REAL | DEFAULT 0 | G (3rd string) gauge |
| GaugeD4 | REAL | DEFAULT 0 | D (4th string) gauge |
| GaugeA5 | REAL | DEFAULT 0 | A (5th string) gauge |
| GaugeE6 | REAL | DEFAULT 0 | Low E (6th string) gauge |
| CreatedAt | TEXT | NOT NULL | ISO 8601 datetime |

### StringBaselines

Initial spectral fingerprint for each string (6 per set). Captured when strings are fresh and used as the reference for decay calculations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Unique identifier |
| SetId | INTEGER | NOT NULL, INDEXED | FK to StringSets.Id |
| StringNumber | INTEGER | NOT NULL | 1-6 (1 = High E, 6 = Low E) |
| FundamentalFreq | REAL | DEFAULT 0 | Fundamental frequency in Hz |
| InitialCentroid | REAL | DEFAULT 0 | Spectral centroid at baseline (Hz) |
| InitialHighRatio | REAL | DEFAULT 0 | High-frequency energy ratio at baseline |
| CreatedAt | TEXT | NOT NULL | ISO 8601 datetime |

### MeasurementLogs

Time-series decay data capturing spectral degradation over the lifetime of a string.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Unique identifier |
| BaselineId | INTEGER | NOT NULL, INDEXED | FK to StringBaselines.Id |
| CurrentCentroid | REAL | DEFAULT 0 | Current spectral centroid (Hz) |
| CurrentHighRatio | REAL | DEFAULT 0 | Current high-frequency energy ratio |
| DecayPercentage | REAL | DEFAULT 0 | `(Initial - Current) / Initial * 100` |
| PlayTimeHours | REAL | DEFAULT 0 | User-reported accumulated play time |
| Note | TEXT | NULLABLE | Optional user annotation |
| MeasuredAt | TEXT | NOT NULL | ISO 8601 datetime |

### Guitars

User's guitar collection with type classification.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Unique identifier |
| Name | TEXT | NOT NULL | User-defined name (e.g., "My Strat") |
| Make | TEXT | NULLABLE | Manufacturer (e.g., "Fender") |
| Model | TEXT | NULLABLE | Model name (e.g., "Stratocaster") |
| Type | TEXT | NOT NULL, DEFAULT "Electric" | "Electric", "Acoustic", "Classical", or "Bass" |
| Notes | TEXT | NULLABLE | Optional notes |
| CreatedAt | TEXT | | ISO 8601 datetime |

### GuitarStringSetPairings

Junction table linking guitars to string sets. Tracks installation history and active status.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Unique identifier |
| GuitarId | INTEGER | INDEXED | FK to Guitars.Id |
| SetId | INTEGER | INDEXED | FK to StringSets.Id |
| InstalledAt | TEXT | | Datetime when strings were installed |
| RemovedAt | TEXT | NULLABLE | Datetime when strings were removed (null = still installed) |
| IsActive | INTEGER | | Boolean: only one pairing per guitar can be active |
| Notes | TEXT | NULLABLE | Optional installation notes |

## Integrity Rules

1. **One active pairing per guitar**: Only one `GuitarStringSetPairing` per `GuitarId` can have `IsActive = true`. Enforced by `GuitarStringSetPairingRepository` which deactivates existing pairings before activating a new one.

2. **Cascade deletes** (manual, in Repository layer):
   - **Guitar delete** (`GuitarRepository`): Deletes all `GuitarStringSetPairings` for that guitar
   - **StringSet delete** (`StringSetRepository`): Deletes all `StringBaselines` (which cascades to `MeasurementLogs`) and all `GuitarStringSetPairings` for that set
   - **StringBaseline delete** (`StringBaselineRepository`): Deletes all `MeasurementLogs` for that baseline

3. **No SQL-level FK constraints**: SQLite FK support is disabled by default and inconsistent across sqlite-net-pcl operations. All referential integrity is managed in C# code.

4. **Thread safety**: All database operations are async via `IDatabaseService`, ensuring non-blocking UI access.

## Seed Data

The `ISeedDataService` populates 33 preset string sets across 11 categories (3 gauge variants each):

| Brand | Series | Type |
|-------|--------|------|
| Ernie Ball | Slinky | Electric |
| D'Addario | XL Nickel | Electric |
| D'Addario | NYXL | Electric |
| Elixir | Optiweb | Electric |
| Elixir | Nanoweb | Electric |
| Elixir | Polyweb | Electric |
| Martin | Authentic 80/20 Bronze | Acoustic |
| D'Addario | Phosphor Bronze (EJ) | Acoustic |
| Elixir | Phosphor Bronze Nanoweb | Acoustic |
| Elixir | 80/20 Bronze Nanoweb | Acoustic |
| Elixir | 80/20 Bronze Polyweb | Acoustic |

## See Also

- [`src/SonicDecay.Data/Schema.sql`](../src/SonicDecay.Data/Schema.sql) - SQL schema definition
- [`src/SonicDecay.Data/SeedData.sql`](../src/SonicDecay.Data/SeedData.sql) - Seed data reference
