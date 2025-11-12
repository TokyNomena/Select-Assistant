using System.Windows.Controls;
using SelectionSet.ViewModels;

namespace SelectionSet.Views
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            this.DataContext = new AboutViewModel();
        }
    }
}
