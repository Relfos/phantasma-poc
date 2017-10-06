using PhantasmaApp.Views;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace PhantasmaApp
{
	public partial class App : Application
	{
        public App()
		{
			InitializeComponent();

			SetMainPage();
		}

        private static NavigationPage CreateMailTab(string filter)
        {
            return new NavigationPage(new ItemsPage(filter))
            {
                Title = filter,
                Icon = Device.OnPlatform("tab_feed.png", null, null)
            };
        }

        public static void SetMainPage()
		{
            Current.MainPage = new TabbedPage
            {
                Children =
                {
                    CreateMailTab("Inbox"),
                    CreateMailTab("Sent"),
                    new NavigationPage(new AboutPage())
                    {
                        Title = "About",
                        Icon = Device.OnPlatform<string>("tab_about.png",null,null)
                    },
                }
            };
        }
	}
}
