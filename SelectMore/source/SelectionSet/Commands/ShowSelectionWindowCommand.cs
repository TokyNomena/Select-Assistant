using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SelectionSet.Views;

namespace SelectionSet.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.ReadOnly)]
    public class ShowSelectionDockPanelCommand : ExternalCommand
    {
        public override void Execute()
        {
            try
            {
                var dockablePane = UiApplication.GetDockablePane(SelectionSetDockPaneProvider.Id);
                SelectionSetDockPaneProvider.Reinitialise(UiApplication);
                if (!dockablePane.IsShown())
                    dockablePane.Show();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                   ex.Message, "Select Assits",
                   System.Windows.MessageBoxButton.OK,
                   System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [UsedImplicitly]
    [Transaction(TransactionMode.ReadOnly)]
    public class ShowSelectionWindowCommand : ExternalCommand
    {
        public override void Execute()
        {
            try
            {
                var ui = new SelectionOptionsView();
                ui.Init(UiApplication.ActiveUIDocument.Document);

                var window = new Window();
                window.Content = ui;
                window.Title = "Selection Assistant";
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                var helper = new WindowInteropHelper(window);
                helper.Owner = UiApplication.MainWindowHandle;
                window.Show();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                   ex.Message, "Selection Assistant",
                   System.Windows.MessageBoxButton.OK,
                   System.Windows.MessageBoxImage.Error);
            }
        }
    }
}