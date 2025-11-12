using Autodesk.Revit.UI;

namespace SelectionSet.ViewModels
{
    internal interface IGeneralFilter
    {
        string Title { get; }
        void Init(Document document);
        IEnumerable<Element> GetFilteredElements(UIApplication app);
    }
}
