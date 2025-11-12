using Autodesk.Revit.UI;

namespace SelectionSet.Adapters
{
    internal interface IUIApplicationAdapter
    {
        IntPtr MainWindowHandle { get; }
        UIDocument ActiveUIDocument { get; }
        UIApplication UnderlyingApp { get; }
    }

    internal class RevitUIApplicationAdapter : IUIApplicationAdapter
    {
        private readonly UIApplication app;
        public RevitUIApplicationAdapter(UIApplication app) { this.app = app; }
        public IntPtr MainWindowHandle => app.MainWindowHandle;
        public UIDocument ActiveUIDocument => app.ActiveUIDocument;
        public UIApplication UnderlyingApp => app;
    }
}
