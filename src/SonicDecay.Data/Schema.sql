-- Sonic Decay Analyzer Database Schema
-- SQLite compatible schema matching sqlite-net-pcl generated tables
-- This file serves as documentation and validation reference

-- ============================================================================
-- Table: StringSets
-- Stores guitar string set metadata (brand, model, individual gauges)
-- ============================================================================
CREATE TABLE IF NOT EXISTS StringSets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Brand TEXT NOT NULL,
    Model TEXT NOT NULL,
    GaugeE1 REAL DEFAULT 0,       -- High E (thinnest) gauge
    GaugeB2 REAL DEFAULT 0,       -- B string gauge
    GaugeG3 REAL DEFAULT 0,       -- G string gauge
    GaugeD4 REAL DEFAULT 0,       -- D string gauge
    GaugeA5 REAL DEFAULT 0,       -- A string gauge
    GaugeE6 REAL DEFAULT 0,       -- Low E (thickest) gauge
    CreatedAt TEXT NOT NULL       -- DateTime stored as TEXT in SQLite
);

-- ============================================================================
-- Table: StringBaselines
-- Stores initial spectral fingerprint for each string (6 per set)
-- Reference metrics captured when strings are fresh
-- ============================================================================
CREATE TABLE IF NOT EXISTS StringBaselines (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SetId INTEGER NOT NULL,           -- FK → StringSets.Id (managed in code)
    StringNumber INTEGER NOT NULL,    -- 1-6 (1=High E, 6=Low E)
    FundamentalFreq REAL DEFAULT 0,   -- Fundamental frequency in Hz
    InitialCentroid REAL DEFAULT 0,   -- Spectral centroid at baseline (Hz)
    InitialHighRatio REAL DEFAULT 0,  -- HF energy ratio at baseline
    CreatedAt TEXT NOT NULL           -- DateTime stored as TEXT in SQLite
);

-- Index for efficient FK lookups on StringBaselines
CREATE INDEX IF NOT EXISTS idx_StringBaselines_SetId ON StringBaselines(SetId);

-- ============================================================================
-- Table: MeasurementLogs
-- Time-series decay data capturing spectral degradation over time
-- ============================================================================
CREATE TABLE IF NOT EXISTS MeasurementLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BaselineId INTEGER NOT NULL,      -- FK → StringBaselines.Id (managed in code)
    CurrentCentroid REAL DEFAULT 0,   -- Current spectral centroid (Hz)
    CurrentHighRatio REAL DEFAULT 0,  -- Current HF energy ratio
    DecayPercentage REAL DEFAULT 0,   -- Calculated: (Initial - Current) / Initial * 100
    PlayTimeHours REAL DEFAULT 0,     -- User-reported accumulated play time
    Note TEXT,                        -- Optional user annotation
    MeasuredAt TEXT NOT NULL          -- DateTime stored as TEXT in SQLite
);

-- Index for efficient FK lookups on MeasurementLogs
CREATE INDEX IF NOT EXISTS idx_MeasurementLogs_BaselineId ON MeasurementLogs(BaselineId);

-- ============================================================================
-- Validation Queries
-- Use these to verify data integrity
-- ============================================================================

-- Check for orphaned baselines (no parent StringSet)
-- SELECT * FROM StringBaselines WHERE SetId NOT IN (SELECT Id FROM StringSets);

-- Check for orphaned measurement logs (no parent Baseline)
-- SELECT * FROM MeasurementLogs WHERE BaselineId NOT IN (SELECT Id FROM StringBaselines);

-- Verify each StringSet has exactly 6 baselines
-- SELECT SetId, COUNT(*) as BaselineCount
-- FROM StringBaselines
-- GROUP BY SetId
-- HAVING BaselineCount != 6;

-- ============================================================================
-- Sample Data for Testing
-- Uncomment to insert test data
-- ============================================================================

-- INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
-- VALUES ('Elixir', 'Nanoweb Light', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now'));

-- ============================================================================
-- Notes on Foreign Key Management
-- ============================================================================
--
-- SQLite does support FOREIGN KEY constraints, but they are:
-- 1. Disabled by default (requires PRAGMA foreign_keys = ON)
-- 2. Not consistently supported across all sqlite-net-pcl operations
--
-- Per CLAUDE.md architectural requirements:
-- - FK relationships are enforced in the Repository layer (C# code)
-- - Cascade deletes are implemented manually:
--   * StringSet delete → deletes all StringBaselines → deletes all MeasurementLogs
--   * StringBaseline delete → deletes all MeasurementLogs
--
-- This approach provides:
-- - Explicit control over deletion order
-- - Consistent behavior across all SQLite versions
-- - Clear error handling in application code
-- ============================================================================
