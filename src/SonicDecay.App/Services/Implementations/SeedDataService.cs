using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Service implementation for seeding the database with preset string set data.
    /// Contains manufacturer presets for common electric and acoustic guitar strings.
    /// </summary>
    public class SeedDataService : ISeedDataService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IStringSetRepository _stringSetRepository;
        private readonly IStringBaselineRepository _baselineRepository;
        private bool _isSeeded;

        /// <summary>
        /// Standard tuning fundamental frequencies for each string (Hz).
        /// </summary>
        private static readonly double[] StandardTuningFrequencies =
        {
            329.63, // E4 - High E (string 1)
            246.94, // B3 (string 2)
            196.00, // G3 (string 3)
            146.83, // D3 (string 4)
            110.00, // A2 (string 5)
            82.41   // E2 - Low E (string 6)
        };

        /// <inheritdoc />
        public bool IsSeeded => _isSeeded;

        /// <summary>
        /// Initializes a new instance of the SeedDataService class.
        /// </summary>
        /// <param name="databaseService">The database service for direct connection access.</param>
        /// <param name="stringSetRepository">The string set repository for data operations.</param>
        /// <param name="baselineRepository">The baseline repository for creating string baselines.</param>
        public SeedDataService(
            IDatabaseService databaseService,
            IStringSetRepository stringSetRepository,
            IStringBaselineRepository baselineRepository)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));
            _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
        }

        /// <inheritdoc />
        public async Task SeedIfEmptyAsync()
        {
            await _databaseService.InitializeAsync();

            var existingSets = await _stringSetRepository.GetAllAsync();
            if (existingSets.Count > 0)
            {
                _isSeeded = true;
                return;
            }

            await SeedPresetsAsync();
            _isSeeded = true;
        }

        /// <inheritdoc />
        public async Task ReseedPresetsAsync()
        {
            await _databaseService.InitializeAsync();

            // Delete all existing string sets (cascade deletes baselines and logs)
            var existingSets = await _stringSetRepository.GetAllAsync();
            foreach (var set in existingSets)
            {
                await _stringSetRepository.DeleteAsync(set.Id);
            }

            await SeedPresetsAsync();
            _isSeeded = true;
        }

        /// <summary>
        /// Seeds all preset string sets into the database.
        /// Creates 6 StringBaselines per StringSet for each guitar string.
        /// </summary>
        private async Task SeedPresetsAsync()
        {
            var presets = GetPresetStringSets();

            foreach (var preset in presets)
            {
                await _stringSetRepository.CreateAsync(preset);

                // Create 6 baselines for each string set (one per guitar string)
                var baselines = new List<StringBaseline>();
                for (int i = 1; i <= 6; i++)
                {
                    baselines.Add(new StringBaseline
                    {
                        SetId = preset.Id,
                        StringNumber = i,
                        FundamentalFreq = StandardTuningFrequencies[i - 1],
                        InitialCentroid = 0, // Will be set when baseline is established
                        InitialHighRatio = 0,
                        CreatedAt = DateTime.Now
                    });
                }

                await _baselineRepository.CreateBatchAsync(baselines);
            }
        }

        /// <summary>
        /// Gets all preset string set definitions.
        /// Data sourced from manufacturer specifications.
        /// </summary>
        /// <returns>A list of preset StringSet objects.</returns>
        private static List<StringSet> GetPresetStringSets()
        {
            return new List<StringSet>
            {
                // ============================================================
                // Ernie Ball Slinky Series (Electric)
                // ============================================================
                new StringSet
                {
                    Brand = "Ernie Ball",
                    Model = "Super Slinky (09-42)",
                    GaugeE1 = 0.009, GaugeB2 = 0.011, GaugeG3 = 0.016,
                    GaugeD4 = 0.024, GaugeA5 = 0.032, GaugeE6 = 0.042
                },
                new StringSet
                {
                    Brand = "Ernie Ball",
                    Model = "Regular Slinky (10-46)",
                    GaugeE1 = 0.010, GaugeB2 = 0.013, GaugeG3 = 0.017,
                    GaugeD4 = 0.026, GaugeA5 = 0.036, GaugeE6 = 0.046
                },
                new StringSet
                {
                    Brand = "Ernie Ball",
                    Model = "Power Slinky (11-48)",
                    GaugeE1 = 0.011, GaugeB2 = 0.014, GaugeG3 = 0.018,
                    GaugeD4 = 0.028, GaugeA5 = 0.038, GaugeE6 = 0.048
                },

                // ============================================================
                // D'Addario XL Nickel Series (Electric)
                // ============================================================
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "XL Nickel Super Light (09-42)",
                    GaugeE1 = 0.009, GaugeB2 = 0.011, GaugeG3 = 0.016,
                    GaugeD4 = 0.024, GaugeA5 = 0.032, GaugeE6 = 0.042
                },
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "XL Nickel Regular Light (10-46)",
                    GaugeE1 = 0.010, GaugeB2 = 0.013, GaugeG3 = 0.017,
                    GaugeD4 = 0.026, GaugeA5 = 0.036, GaugeE6 = 0.046
                },
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "XL Nickel Medium (11-48)",
                    GaugeE1 = 0.011, GaugeB2 = 0.014, GaugeG3 = 0.018,
                    GaugeD4 = 0.028, GaugeA5 = 0.038, GaugeE6 = 0.049
                },

                // ============================================================
                // Elixir Optiweb Series (Electric)
                // ============================================================
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Optiweb Light (09-42)",
                    GaugeE1 = 0.009, GaugeB2 = 0.011, GaugeG3 = 0.016,
                    GaugeD4 = 0.024, GaugeA5 = 0.032, GaugeE6 = 0.042
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Optiweb Custom Light (10-46)",
                    GaugeE1 = 0.010, GaugeB2 = 0.013, GaugeG3 = 0.017,
                    GaugeD4 = 0.026, GaugeA5 = 0.036, GaugeE6 = 0.046
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Optiweb Medium (11-49)",
                    GaugeE1 = 0.011, GaugeB2 = 0.014, GaugeG3 = 0.018,
                    GaugeD4 = 0.028, GaugeA5 = 0.038, GaugeE6 = 0.049
                },

                // ============================================================
                // Elixir Nanoweb Series (Electric)
                // ============================================================
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Nanoweb Light (09-42)",
                    GaugeE1 = 0.009, GaugeB2 = 0.011, GaugeG3 = 0.016,
                    GaugeD4 = 0.024, GaugeA5 = 0.032, GaugeE6 = 0.042
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Nanoweb Custom Light (10-46)",
                    GaugeE1 = 0.010, GaugeB2 = 0.013, GaugeG3 = 0.017,
                    GaugeD4 = 0.026, GaugeA5 = 0.036, GaugeE6 = 0.046
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Nanoweb Medium (11-49)",
                    GaugeE1 = 0.011, GaugeB2 = 0.014, GaugeG3 = 0.018,
                    GaugeD4 = 0.028, GaugeA5 = 0.038, GaugeE6 = 0.049
                },

                // ============================================================
                // Elixir Polyweb Series (Electric)
                // ============================================================
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Polyweb Light (09-42)",
                    GaugeE1 = 0.009, GaugeB2 = 0.011, GaugeG3 = 0.016,
                    GaugeD4 = 0.024, GaugeA5 = 0.032, GaugeE6 = 0.042
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Polyweb Custom Light (10-46)",
                    GaugeE1 = 0.010, GaugeB2 = 0.013, GaugeG3 = 0.017,
                    GaugeD4 = 0.026, GaugeA5 = 0.036, GaugeE6 = 0.046
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "Polyweb Medium (11-49)",
                    GaugeE1 = 0.011, GaugeB2 = 0.014, GaugeG3 = 0.018,
                    GaugeD4 = 0.028, GaugeA5 = 0.038, GaugeE6 = 0.049
                },

                // ============================================================
                // D'Addario NYXL Series (Electric)
                // ============================================================
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "NYXL Super Light (09-42)",
                    GaugeE1 = 0.009, GaugeB2 = 0.011, GaugeG3 = 0.016,
                    GaugeD4 = 0.024, GaugeA5 = 0.032, GaugeE6 = 0.042
                },
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "NYXL Regular Light (10-46)",
                    GaugeE1 = 0.010, GaugeB2 = 0.013, GaugeG3 = 0.017,
                    GaugeD4 = 0.026, GaugeA5 = 0.036, GaugeE6 = 0.046
                },
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "NYXL Medium (11-49)",
                    GaugeE1 = 0.011, GaugeB2 = 0.014, GaugeG3 = 0.018,
                    GaugeD4 = 0.028, GaugeA5 = 0.038, GaugeE6 = 0.049
                },

                // ============================================================
                // Martin 80/20 Bronze Series (Acoustic)
                // ============================================================
                new StringSet
                {
                    Brand = "Martin",
                    Model = "Authentic 80/20 Custom Light (11-52)",
                    GaugeE1 = 0.011, GaugeB2 = 0.015, GaugeG3 = 0.023,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.052
                },
                new StringSet
                {
                    Brand = "Martin",
                    Model = "Authentic 80/20 Light (12-54)",
                    GaugeE1 = 0.012, GaugeB2 = 0.016, GaugeG3 = 0.025,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.054
                },
                new StringSet
                {
                    Brand = "Martin",
                    Model = "Authentic 80/20 Medium (13-56)",
                    GaugeE1 = 0.013, GaugeB2 = 0.017, GaugeG3 = 0.026,
                    GaugeD4 = 0.035, GaugeA5 = 0.045, GaugeE6 = 0.056
                },

                // ============================================================
                // D'Addario Phosphor Bronze Series (Acoustic)
                // ============================================================
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "EJ13 Custom Light (11-52)",
                    GaugeE1 = 0.011, GaugeB2 = 0.015, GaugeG3 = 0.022,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.052
                },
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "EJ16 Light (12-53)",
                    GaugeE1 = 0.012, GaugeB2 = 0.016, GaugeG3 = 0.024,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.053
                },
                new StringSet
                {
                    Brand = "D'Addario",
                    Model = "EJ17 Medium (13-56)",
                    GaugeE1 = 0.013, GaugeB2 = 0.017, GaugeG3 = 0.026,
                    GaugeD4 = 0.035, GaugeA5 = 0.045, GaugeE6 = 0.056
                },

                // ============================================================
                // Elixir Phosphor Bronze Nanoweb Series (Acoustic)
                // ============================================================
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "PB Nanoweb Extra Light (10-47)",
                    GaugeE1 = 0.010, GaugeB2 = 0.014, GaugeG3 = 0.023,
                    GaugeD4 = 0.030, GaugeA5 = 0.039, GaugeE6 = 0.047
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "PB Nanoweb Custom Light (11-52)",
                    GaugeE1 = 0.011, GaugeB2 = 0.015, GaugeG3 = 0.022,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.052
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "PB Nanoweb Light (12-53)",
                    GaugeE1 = 0.012, GaugeB2 = 0.016, GaugeG3 = 0.024,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.053
                },

                // ============================================================
                // Elixir 80/20 Bronze Nanoweb Series (Acoustic)
                // ============================================================
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "80/20 Nanoweb Extra Light (10-47)",
                    GaugeE1 = 0.010, GaugeB2 = 0.014, GaugeG3 = 0.023,
                    GaugeD4 = 0.030, GaugeA5 = 0.039, GaugeE6 = 0.047
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "80/20 Nanoweb Custom Light (11-52)",
                    GaugeE1 = 0.011, GaugeB2 = 0.015, GaugeG3 = 0.022,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.052
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "80/20 Nanoweb Light (12-53)",
                    GaugeE1 = 0.012, GaugeB2 = 0.016, GaugeG3 = 0.024,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.053
                },

                // ============================================================
                // Elixir 80/20 Bronze Polyweb Series (Acoustic)
                // ============================================================
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "80/20 Polyweb Extra Light (10-47)",
                    GaugeE1 = 0.010, GaugeB2 = 0.014, GaugeG3 = 0.023,
                    GaugeD4 = 0.030, GaugeA5 = 0.039, GaugeE6 = 0.047
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "80/20 Polyweb Custom Light (11-52)",
                    GaugeE1 = 0.011, GaugeB2 = 0.015, GaugeG3 = 0.022,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.052
                },
                new StringSet
                {
                    Brand = "Elixir",
                    Model = "80/20 Polyweb Light (12-53)",
                    GaugeE1 = 0.012, GaugeB2 = 0.016, GaugeG3 = 0.024,
                    GaugeD4 = 0.032, GaugeA5 = 0.042, GaugeE6 = 0.053
                }
            };
        }
    }
}
