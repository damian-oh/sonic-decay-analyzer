using SonicDecay.App.Views;

namespace SonicDecay.App
{
    /// <summary>
    /// Application shell defining navigation structure.
    /// </summary>
    public partial class AppShell : Shell
    {
        /// <summary>
        /// Initializes a new instance of the AppShell class.
        /// </summary>
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute("LibraryPage", typeof(LibraryPage));
            Routing.RegisterRoute("StringInputPage", typeof(StringInputPage));
            Routing.RegisterRoute("GuitarInputPage", typeof(GuitarInputPage));
            Routing.RegisterRoute("DecayChartPage", typeof(DecayChartPage));
            Routing.RegisterRoute("PairingsManagementPage", typeof(PairingsManagementPage));
        }
    }
}
