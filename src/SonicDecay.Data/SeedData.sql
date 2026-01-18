-- Sonic Decay Analyzer: Seed Data for StringSets
-- This SQL file serves as reference documentation for preset string data.
-- Actual seeding is performed by ISeedDataService in C# at runtime.
-- ============================================================================

-- Ernie Ball Slinky Series (Electric)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Ernie Ball', 'Super Slinky (09-42)', 0.009, 0.011, 0.016, 0.024, 0.032, 0.042, datetime('now')),
    ('Ernie Ball', 'Regular Slinky (10-46)', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now')),
    ('Ernie Ball', 'Power Slinky (11-48)', 0.011, 0.014, 0.018, 0.028, 0.038, 0.048, datetime('now'));

-- D'Addario XL Nickel Series (Electric)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('D''Addario', 'XL Nickel Super Light (09-42)', 0.009, 0.011, 0.016, 0.024, 0.032, 0.042, datetime('now')),
    ('D''Addario', 'XL Nickel Regular Light (10-46)', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now')),
    ('D''Addario', 'XL Nickel Medium (11-48)', 0.011, 0.014, 0.018, 0.028, 0.038, 0.049, datetime('now'));

-- Elixir Optiweb Series (Electric)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Elixir', 'Optiweb Light (09-42)', 0.009, 0.011, 0.016, 0.024, 0.032, 0.042, datetime('now')),
    ('Elixir', 'Optiweb Custom Light (10-46)', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now')),
    ('Elixir', 'Optiweb Medium (11-49)', 0.011, 0.014, 0.018, 0.028, 0.038, 0.049, datetime('now'));

-- Elixir Nanoweb Series (Electric)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Elixir', 'Nanoweb Light (09-42)', 0.009, 0.011, 0.016, 0.024, 0.032, 0.042, datetime('now')),
    ('Elixir', 'Nanoweb Custom Light (10-46)', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now')),
    ('Elixir', 'Nanoweb Medium (11-49)', 0.011, 0.014, 0.018, 0.028, 0.038, 0.049, datetime('now'));

-- Elixir Polyweb Series (Electric)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Elixir', 'Polyweb Light (09-42)', 0.009, 0.011, 0.016, 0.024, 0.032, 0.042, datetime('now')),
    ('Elixir', 'Polyweb Custom Light (10-46)', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now')),
    ('Elixir', 'Polyweb Medium (11-49)', 0.011, 0.014, 0.018, 0.028, 0.038, 0.049, datetime('now'));

-- D'Addario NYXL Series (Electric)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('D''Addario', 'NYXL Super Light (09-42)', 0.009, 0.011, 0.016, 0.024, 0.032, 0.042, datetime('now')),
    ('D''Addario', 'NYXL Regular Light (10-46)', 0.010, 0.013, 0.017, 0.026, 0.036, 0.046, datetime('now')),
    ('D''Addario', 'NYXL Medium (11-49)', 0.011, 0.014, 0.018, 0.028, 0.038, 0.049, datetime('now'));

-- Martin 80/20 Bronze Series (Acoustic)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Martin', 'Authentic 80/20 Custom Light (11-52)', 0.011, 0.015, 0.023, 0.032, 0.042, 0.052, datetime('now')),
    ('Martin', 'Authentic 80/20 Light (12-54)', 0.012, 0.016, 0.025, 0.032, 0.042, 0.054, datetime('now')),
    ('Martin', 'Authentic 80/20 Medium (13-56)', 0.013, 0.017, 0.026, 0.035, 0.045, 0.056, datetime('now'));

-- D'Addario Phosphor Bronze Series (Acoustic)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('D''Addario', 'EJ13 Custom Light (11-52)', 0.011, 0.015, 0.022, 0.032, 0.042, 0.052, datetime('now')),
    ('D''Addario', 'EJ16 Light (12-53)', 0.012, 0.016, 0.024, 0.032, 0.042, 0.053, datetime('now')),
    ('D''Addario', 'EJ17 Medium (13-56)', 0.013, 0.017, 0.026, 0.035, 0.045, 0.056, datetime('now'));

-- Elixir Phosphor Bronze Nanoweb Series (Acoustic)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Elixir', 'PB Nanoweb Extra Light (10-47)', 0.010, 0.014, 0.023, 0.030, 0.039, 0.047, datetime('now')),
    ('Elixir', 'PB Nanoweb Custom Light (11-52)', 0.011, 0.015, 0.022, 0.032, 0.042, 0.052, datetime('now')),
    ('Elixir', 'PB Nanoweb Light (12-53)', 0.012, 0.016, 0.024, 0.032, 0.042, 0.053, datetime('now'));

-- Elixir 80/20 Bronze Nanoweb Series (Acoustic)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Elixir', '80/20 Nanoweb Extra Light (10-47)', 0.010, 0.014, 0.023, 0.030, 0.039, 0.047, datetime('now')),
    ('Elixir', '80/20 Nanoweb Custom Light (11-52)', 0.011, 0.015, 0.022, 0.032, 0.042, 0.052, datetime('now')),
    ('Elixir', '80/20 Nanoweb Light (12-53)', 0.012, 0.016, 0.024, 0.032, 0.042, 0.053, datetime('now'));

-- Elixir 80/20 Bronze Polyweb Series (Acoustic)
INSERT INTO StringSets (Brand, Model, GaugeE1, GaugeB2, GaugeG3, GaugeD4, GaugeA5, GaugeE6, CreatedAt)
VALUES
    ('Elixir', '80/20 Polyweb Extra Light (10-47)', 0.010, 0.014, 0.023, 0.030, 0.039, 0.047, datetime('now')),
    ('Elixir', '80/20 Polyweb Custom Light (11-52)', 0.011, 0.015, 0.022, 0.032, 0.042, 0.052, datetime('now')),
    ('Elixir', '80/20 Polyweb Light (12-53)', 0.012, 0.016, 0.024, 0.032, 0.042, 0.053, datetime('now'));
