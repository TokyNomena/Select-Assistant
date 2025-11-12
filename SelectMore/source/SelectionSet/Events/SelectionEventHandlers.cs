using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SelectionSet.Adapters;
using SelectionSet.ViewModels;

namespace SelectionSet.Events
{
    internal class TakeSampleEventHandler : IExternalEventHandler
    {
        private readonly WeakReference<SelectionOptionsViewModel> model;
        private readonly IUIApplicationAdapter? uiAdapter;

        public TakeSampleEventHandler(SelectionOptionsViewModel model) : this(model, null) { }
        public TakeSampleEventHandler(SelectionOptionsViewModel model, IUIApplicationAdapter? uiAdapter)
        {
            this.model = new WeakReference<SelectionOptionsViewModel>(model);
            this.uiAdapter = uiAdapter;
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var adapter = uiAdapter ?? new RevitUIApplicationAdapter(app);
                var activeDoc = adapter.ActiveUIDocument ?? app.ActiveUIDocument;
                var reference = activeDoc.Selection.PickObject(
                Autodesk.Revit.UI.Selection.ObjectType.Element,
                SelectionSet.Properties.Resources.LabelSelectSample);
                if (reference == null)
                    return;

                var document = activeDoc.Document;
                var element = document.GetElement(reference);
                if (element == null)
                    return;

                PopulateFromElement(element);

                var elementType = document.GetElement(element.GetTypeId());
                if (elementType == null)
                    return;

                PopulateFromElement(elementType);

            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void PopulateFromElement(Element element)
        {
            foreach (Parameter paramater in element.Parameters)
            {
                if (paramater == null)
                    continue;

                var filter = new ParameterSelectionFilterViewModel(paramater);
                if (model.TryGetTarget(out var target) && target != null)
                {
                    target.Filters.Add(filter);
                }
            }
        }

        public string GetName()
        {
            return "Selection Assistant:TakeSampleEventHandler";
        }
    }

    internal class PickByRectangleEventHandler : IExternalEventHandler
    {
        private readonly WeakReference<SelectionOptionsViewModel> model;

        public PickByRectangleEventHandler(SelectionOptionsViewModel selectionOptionsViewModel)
        {
            model = new WeakReference<SelectionOptionsViewModel>(selectionOptionsViewModel);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                if (model.TryGetTarget(out var target) && target != null)
                {
                    var selection = new CustomnSelectionFilterByApp(
                    app.ActiveUIDocument.Document, target.Filters);
                    var reference = app.ActiveUIDocument.Selection.PickElementsByRectangle(
                    selection,
                    SelectionSet.Properties.Resources.LabelSelectElementsByRectangle);
                    if (reference == null) return;

                    app.ActiveUIDocument.Selection.SetElementIds(reference.Select(f => f.Id).ToList());
                    target.SetNumberOfSelectedElements((uint)reference.Count);
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public string GetName()
        {
            return "Selection Assistant:PickByRectangle";
        }
    }

    internal class ApplySelectionEventHandler : IExternalEventHandler
    {
        private readonly SelectionHandlerAction action;
        private readonly WeakReference<SelectionOptionsViewModel> model;

        public ApplySelectionEventHandler(SelectionHandlerAction action, SelectionOptionsViewModel selectionOptionsViewModel)
        {
            this.action = action;
            model = new WeakReference<SelectionOptionsViewModel>(selectionOptionsViewModel);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                if (model.TryGetTarget(out var target) && target != null)
                {
                    HashSet<ElementId> filtered = target.GetFiltererdElement(app).ToHashSet();
                    List<ElementId> ids = new List<ElementId>();
                    switch (action)
                    {
                        case SelectionHandlerAction.Apply:
                            ids.AddRange(filtered);
                            break;
                        case SelectionHandlerAction.Add:
                            ids.AddRange(app.ActiveUIDocument.Selection.GetElementIds().Concat(filtered.ToList()));
                            break;
                        case SelectionHandlerAction.Remove:
                            ids.AddRange(app.ActiveUIDocument.Selection.GetElementIds().Where(f => !filtered.Contains(f)));
                            break;
                    }
                    app.ActiveUIDocument.Selection.SetElementIds(ids);
                    target.SetNumberOfSelectedElements((uint)ids.Count);

                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public string GetName()
        {
            return "Selection Assistant:PickByRectangle";
        }
    }

    internal class SelectRoomEventHandler : IExternalEventHandler
    {
        public WeakReference<ByRoomGeneralFilter> Filter { get; }

        public SelectRoomEventHandler(ByRoomGeneralFilter filter)
        {
            Filter = new WeakReference<ByRoomGeneralFilter>(filter);
        }

        private class SelectionFilter : ISelectionFilter
        {
            private readonly HashSet<ElementId> roomsId;

            public SelectionFilter(IEnumerable<ElementId> roomsId)
            {
                this.roomsId = new HashSet<ElementId>(roomsId);
            }

            public bool AllowElement(Element elem)
            {
                return roomsId.Contains(elem.Id);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var rooms = new FilteredElementCollector(app.ActiveUIDocument.Document)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WherePasses(new RoomFilter())
                .ToElementIds();

                if (Filter.TryGetTarget(out var target) && target != null)
                {
                    var references = target.Rooms
                    .Select(f => app.ActiveUIDocument.Document.GetElement(f.Item1))
                    .Where(f => f != null)
                    .Select(f => new Reference(f));

                    var elements = app.ActiveUIDocument.Selection.PickObjects(
                    ObjectType.Element,
                    new SelectionFilter(rooms),
                    SelectionSet.Properties.Resources.LabelSelectRooms,
                    references.ToList());

                    var finalElements = elements
                    .Select(f => app.ActiveUIDocument.Document.GetElement(f))
                    .Where(f => f != null);

                    target.Rooms.Clear();
                    foreach (var item in finalElements)
                    {
                        target.Rooms.Add(new Tuple<ElementId, string>(item.Id, item.Name));
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public string GetName()
        {
            return "Select rooms";
        }
    }
}
