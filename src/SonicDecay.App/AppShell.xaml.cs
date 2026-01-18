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
            Routing.RegisterRoute("StringInputPage", typeof(StringInputPage));
        }
    }
}
