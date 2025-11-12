using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

using Nice3point.Revit.Toolkit.External;

using SelectionSet.Commands;
using SelectionSet.Views;

namespace SelectionSet
{

    public class SelectionSetDockPaneProvider : IDockablePaneProvider
    {
        public const string DockSelectionSetGuid = "D8BBED11-3115-4154-B4E7-B8FA7F6965C0";

        private readonly static SelectionOptionsView view = new SelectionOptionsView();

        private static SelectionSetDockPaneProvider? _instance;
        public static SelectionSetDockPaneProvider Instance => _instance ??= new SelectionSetDockPaneProvider();

        public SelectionSetDockPaneProvider()
        {

        }

        public static DockablePaneId Id => new DockablePaneId(new Guid(DockSelectionSetGuid));

        public static void Reinitialise(UIApplication app)
        {
            view?.Init(app.ActiveUIDocument.Document);
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = view;
            data.InitialState = new DockablePaneState()
            {
                DockPosition = DockPosition.Left,
            };
            data.EditorInteraction = new EditorInteraction()
            {
                InteractionType = EditorInteractionType.KeepAlive
            };
        }

        public void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            Reinit(e.Document);
        }

        public static void Reinit(Document document)
        {
            try
            {
                view.Init(document);
            }
            catch
            {

            }
        }

        public void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            Reinit(e.GetDocument());
        }

        public void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            Reinit(e.Document);
        }
    }

    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            RegisterDockable(Application);
            CreateRibbon(Application);
            Result = Result.Succeeded;
        }

        public override void OnShutdown()
        {
            try
            {
                var pane = SelectionSetDockPaneProvider.Instance;
                Application.ViewActivated -= pane.OnViewActivated;
                Application.ControlledApplication.DocumentChanged -= pane.OnDocumentChanged;
                Application.ControlledApplication.DocumentOpened -= pane.OnDocumentOpened;
            }
            catch { }

            Result = Result.Succeeded;
        }

        private static void RegisterDockable(UIControlledApplication application)
        {
            var pane = SelectionSetDockPaneProvider.Instance;
            application.RegisterDockablePane(
                SelectionSetDockPaneProvider.Id,
                "Select Assits",
                pane);

            application.ViewActivated += pane.OnViewActivated;
            application.ControlledApplication.DocumentChanged += pane.OnDocumentChanged;
            application.ControlledApplication.DocumentOpened += pane.OnDocumentOpened;
        }


        private static void CreateRibbon(UIControlledApplication application)
        {
            var panel = application.CreatePanel("Select Assistant");

            var pullButton = panel.AddPullDownButton("Selection");
            pullButton.SetImage("/SelectionSet;component/Resources/Icons/selectRibbonIcon16.png")
                .SetLargeImage("/SelectionSet;component/Resources/Icons/selectRibbonIcon32.png");

            pullButton
                .AddPushButton<ShowSelectionWindowCommand>("Mode Fenêtre")
                .SetImage("/SelectionSet;component/Resources/Icons/window-display-16.png")
                .SetLargeImage("/SelectionSet;component/Resources/Icons/window-display-32.png");

            pullButton
                .AddPushButton<ShowSelectionDockPanelCommand>("Mode Panel")
                .SetImage("/SelectionSet;component/Resources/Icons/layouting-16.png")
                .SetLargeImage("/SelectionSet;component/Resources/Icons/layouting-32.png");
        }
    }
}