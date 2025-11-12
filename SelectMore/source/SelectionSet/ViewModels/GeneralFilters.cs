using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using MaterialDesignThemes.Wpf;
using SelectionSet.Adapters;
using SelectionSet.Events;
using SelectionSet.Helpers;
using SelectionSet.Inputs;
using SelectionSet.Utils;

namespace SelectionSet.ViewModels
{
    internal partial class ByCategoryGeneralFilter : ObservableObject, IGeneralFilter
    {

        [ObservableProperty]
        private string searchText = "";

        public ObservableCollection<CheckableObject<Tuple<BuiltInCategory, string, int>>> Categories { get; }

        public ObservableCollection<CheckableObject<Tuple<BuiltInCategory, string, int>>> FilteredCategories { get; }

        public ObservableCollection<CheckableObject<Tuple<BuiltInCategory, string, int>>> SelectedCategories { get; }

        public string Title => SelectionSet.Properties.Resources.LabelByCategory;

        public ByCategoryGeneralFilter()
        {
            Categories = new ObservableCollection<CheckableObject<Tuple<BuiltInCategory, string, int>>>();
            FilteredCategories = new ObservableCollection<CheckableObject<Tuple<BuiltInCategory, string, int>>>();
            SelectedCategories = new ObservableCollection<CheckableObject<Tuple<BuiltInCategory, string, int>>>();

            // Listen for search changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchText))
                {
                    UpdateFilteredCategories();
                }
            };
        }

        public void Init(Document document)
        {
            Categories.Clear();

            var categoryCounts = new Dictionary<BuiltInCategory, int>();

            foreach (var element in new FilteredElementCollector(document).WhereElementIsNotElementType())
            {
                if (element.Category == null)
                    continue;

#if NET8_0_OR_GREATER
                var builtInCat = (BuiltInCategory)element.Category.Id.Value;
#else
 var builtInCat = (BuiltInCategory)element.Category?.Id.IntegerValue;
#endif
                if (categoryCounts.ContainsKey(builtInCat)) { categoryCounts[builtInCat]++; } else { categoryCounts[builtInCat] = 1; }
            }

            foreach (var kvp in categoryCounts.OrderBy(x => LabelGenerator.GetLabel(document, x.Key)))
            {
                var categoryName = LabelGenerator.GetLabel(document, kvp.Key);
                var item = new CheckableObject<Tuple<BuiltInCategory, string, int>>(
                new Tuple<BuiltInCategory, string, int>(kvp.Key, categoryName, kvp.Value)
                );

                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CheckableObject<Tuple<BuiltInCategory, string, int>>.IsChecked))
                    {
                        UpdateSelectedCategories();
                    }
                };

                Categories.Add(item);
            }

            UpdateFilteredCategories();
        }

        private void UpdateFilteredCategories()
        {
            FilteredCategories.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText) ? Categories : Categories.Where(c => c.Data.Item2.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var item in filtered) { FilteredCategories.Add(item); }
        }

        private void UpdateSelectedCategories()
        {
            SelectedCategories.Clear();
            foreach (var cat in Categories.Where(c => c.IsChecked)) { SelectedCategories.Add(cat); }
        }

        [RelayCommand]
        private void SelectAllCategories() { foreach (var cat in FilteredCategories) { cat.IsChecked = true; } }

        [RelayCommand]
        private void DeselectAllCategories() { foreach (var cat in Categories) { cat.IsChecked = false; } }

        [RelayCommand]
        private void InvertCategories() { foreach (var cat in FilteredCategories) { cat.IsChecked = !cat.IsChecked; } }

        [RelayCommand]
        private void ClearSearch() { SearchText = ""; }

        [RelayCommand]
        private void ToggleCategory(CheckableObject<Tuple<BuiltInCategory, string, int>> category) { category.IsChecked = !category.IsChecked; }

        [RelayCommand]
        private void RemoveCategory(CheckableObject<Tuple<BuiltInCategory, string, int>> category) { category.IsChecked = false; }

        public IEnumerable<Element> GetFilteredElements(UIApplication app)
        {
            var selectedCategories = Categories.Where(c => c.IsChecked).Select(c => c.Data.Item1).ToList();
            if (selectedCategories.Count == 0) return Enumerable.Empty<Element>();
            var elements = new HashSet<Element>();
            foreach (var category in selectedCategories)
            {
                foreach (var el in new FilteredElementCollector(app.ActiveUIDocument.Document).WhereElementIsNotElementType().OfCategory(category).ToElements())
                {
                    elements.Add(el);
                }
            }
            return elements;
        }
    }

    internal partial class VisibleOnlyOnActiveViewFilter : IGeneralFilter
    {
        public string Title => SelectionSet.Properties.Resources.LabelOnlyVisibleElementOnView;
        public IEnumerable<Element> GetFilteredElements(UIApplication app)
        {
            var view = app.ActiveUIDocument.ActiveView;
            if (view == null)
                throw new InvalidOperationException("Aucune vue active actuellement, veuillez ouvrir une vue");
            return new FilteredElementCollector(app.ActiveUIDocument.Document, view.Id).WhereElementIsNotElementType().ToElements();
        }
        public void Init(Document document) { }
    }

    internal partial class OnLevelGeneralFilter : ObservableObject, IGeneralFilter
    {
        public ObservableCollection<CheckableObject<Tuple<ElementId, string>>> Levels { get; }
        public string Title => SelectionSet.Properties.Resources.LabelOnSelectedLevels;
        public OnLevelGeneralFilter() { Levels = new ObservableCollection<CheckableObject<Tuple<ElementId, string>>>(); }
        public void Init(Document document)
        {
            Levels.Clear();
            foreach (var level in new FilteredElementCollector(document).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Levels).OfClass(typeof(Level)))
            {
                Levels.Add(new CheckableObject<Tuple<ElementId, string>>(new Tuple<ElementId, string>(level.Id, level.Name)));
            }
        }
        public IEnumerable<Element> GetFilteredElements(UIApplication app)
        {
            var levelsIdSelected = Levels.Where((Func<CheckableObject<Tuple<ElementId, string>>, bool>)(f => (bool)f.IsChecked)).Select(f => f.Data.Item1).ToList();
            if (levelsIdSelected.Count == 0) return Enumerable.Empty<Element>();
            var ids = new HashSet<Element>();
            foreach (var levelId in levelsIdSelected)
            {
                foreach (var el in new FilteredElementCollector(app.ActiveUIDocument.Document).WhereElementIsNotElementType().WherePasses(new ElementLevelFilter(levelId)).ToElements()) { ids.Add(el); }
            }
            return ids;
        }
    }

    internal partial class ByRoomGeneralFilter : ObservableObject, IGeneralFilter
    {
        private readonly SelectRoomEventHandler eventHandler;
        private readonly IExternalEventWrapper externalEvent;

        [ObservableProperty]
        private bool methodRoomIsChecked;
        [ObservableProperty]
        private bool methodFromRoomIsChecked;
        [ObservableProperty]
        private bool methodToRoomIsChecked;
        [ObservableProperty]
        private bool fullCollisionContact;
        [ObservableProperty]
        private bool surfacicCollisionContact;

        public ObservableCollection<Tuple<ElementId, string>> Rooms { get; }

        public ByRoomGeneralFilter() : this(null) { }

        public ByRoomGeneralFilter(IExternalEventFactory? factory)
        {
            Rooms = new ObservableCollection<Tuple<ElementId, string>>();
            eventHandler = new SelectRoomEventHandler(this);
            var f = factory ?? new RevitExternalEventFactory();
            externalEvent = f.Create(eventHandler);
        }

        public string Title => SelectionSet.Properties.Resources.LabelRooms;
        public void Init(Document document) { }

        public IEnumerable<Element> GetFilteredElements(UIApplication app)
        {
            if (Rooms.Count == 0) return Enumerable.Empty<Element>();
            var elements = new HashSet<ElementId>();
            if (MethodRoomIsChecked)
            {
                foreach (var element in new FilteredElementCollector(app.ActiveUIDocument.Document).WhereElementIsNotElementType())
                {
                    if (elements.Contains(element.Id)) continue;
                    Room? room = null;
                    if (element is FamilyInstance instance) { try { room = instance.Room; } catch { } }
                    if (room != null && Rooms.Any(f => f.Item1 == room.Id)) elements.Add(element.Id);
                }
            }

            if (MethodToRoomIsChecked)
            {
                foreach (var element in new FilteredElementCollector(app.ActiveUIDocument.Document).WhereElementIsNotElementType())
                {
                    if (elements.Contains(element.Id)) continue;
                    Room? room = null;
                    if (element is FamilyInstance instance) { try { room = instance.ToRoom; } catch { } }
                    if (room != null && Rooms.Any(f => f.Item1 == room.Id)) elements.Add(element.Id);
                }
            }

            if (MethodFromRoomIsChecked)
            {
                foreach (var element in new FilteredElementCollector(app.ActiveUIDocument.Document).WhereElementIsNotElementType())
                {
                    if (elements.Contains(element.Id)) continue;
                    Room? room = null;
                    if (element is FamilyInstance instance) { try { room = instance.FromRoom; } catch { } }
                    if (room != null && Rooms.Any(f => f.Item1 == room.Id)) elements.Add(element.Id);
                }
            }

            if (SurfacicCollisionContact || FullCollisionContact)
            {
                foreach (var tuple in Rooms)
                {
                    Document document = app.ActiveUIDocument.Document;
                    var room = document.GetElement(tuple.Item1) as Room;
                    if (room == null) continue;
                    try
                    {
                        var contacts = room.GetElementsInContactWithRoom();
                        foreach (var contact in contacts)
                        {
                            if (contact.Value == ContactType.SurfaceContact && SurfacicCollisionContact) elements.Add(contact.Key.Id);
                            if (contact.Value == ContactType.VolumetricCollision && FullCollisionContact) elements.Add(contact.Key.Id);
                        }
                    }
                    catch
                    {
                        throw new InvalidProgramException($"Calcul de collision impossible pour la pièce <{tuple.Item1}>");
                    }
                }
            }

            return elements.Select(f => app.ActiveUIDocument.Document.GetElement(f));
        }

        public ICommand SelectRoomsCommand => new ExternalEventUiCommand(externalEvent);
        public ICommand ClearRoomsCommand => new ActionUiCommand((p) => true, (p) => { try { Rooms.Clear(); } catch { } });
    }

    internal partial class GeneralFilterOptionViewModel : ObservableObject
    {
        public IGeneralFilter Filter { get; set; }
        public GeneralFilterOptionViewModel(IGeneralFilter filter) { Filter = filter; }
        [ObservableProperty]
        private bool isActive;
    }

    internal partial class SelectionOptionsViewModel : ObservableObject, IDisposable
    {
        private readonly TakeSampleEventHandler takeSampleEventHandler;
        private readonly IExternalEventWrapper takeSampleExternalEvent;

        private readonly ApplySelectionEventHandler applyNewSelectionEventHandler;
        private readonly IExternalEventWrapper applyNewExternalEvent;

        private readonly ApplySelectionEventHandler applyAddSelectionEventHandler;
        private readonly IExternalEventWrapper applyAddExternalEvent;

        private readonly ApplySelectionEventHandler applyRemoveSelectionEventHandler;
        private readonly IExternalEventWrapper applyRemoveExternalEvent;

        private readonly PickByRectangleEventHandler pickByRectangleEventHandler;
        private readonly IExternalEventWrapper pickByRectangleExternalEvent;

        private bool disposedValue;

        public SelectionOptionsViewModel() : this(null) { }

        public SelectionOptionsViewModel(IExternalEventFactory? factory)
        {
            var f = factory ?? new RevitExternalEventFactory();

            Filters = new ObservableCollection<ISelectionFilterViewModel>();
            selectedFilters = new ObservableCollection<ISelectionFilterViewModel>();
            PreElementsFilter = new ObservableCollection<GeneralFilterOptionViewModel>() {
 new GeneralFilterOptionViewModel(new ByCategoryGeneralFilter()),
 new GeneralFilterOptionViewModel(new VisibleOnlyOnActiveViewFilter()),
 new GeneralFilterOptionViewModel(new OnLevelGeneralFilter()),
 new GeneralFilterOptionViewModel(new ByRoomGeneralFilter(f)),
 };
            SnackbarMessageQueue = new SnackbarMessageQueue();

            takeSampleEventHandler = new TakeSampleEventHandler(this);
            takeSampleExternalEvent = f.Create(takeSampleEventHandler);
            TakeSampleCommand = new ExternalEventUiCommand(takeSampleExternalEvent);

            pickByRectangleEventHandler = new PickByRectangleEventHandler(this);
            pickByRectangleExternalEvent = f.Create(pickByRectangleEventHandler);
            ManualSelectionCommand = new ExternalEventUiCommand(pickByRectangleExternalEvent);

            applyNewSelectionEventHandler = new ApplySelectionEventHandler(SelectionHandlerAction.Apply, this);
            applyNewExternalEvent = f.Create(applyNewSelectionEventHandler);
            NewSelectionCommand = new ExternalEventUiCommand(applyNewExternalEvent);

            applyAddSelectionEventHandler = new ApplySelectionEventHandler(SelectionHandlerAction.Add, this);
            applyAddExternalEvent = f.Create(applyAddSelectionEventHandler);
            AddToSelectionCommand = new ExternalEventUiCommand(applyAddExternalEvent);

            applyRemoveSelectionEventHandler = new ApplySelectionEventHandler(SelectionHandlerAction.Remove, this);
            applyRemoveExternalEvent = f.Create(applyRemoveSelectionEventHandler);
            RemoveSelectionCommand = new ExternalEventUiCommand(applyRemoveExternalEvent);

            DeleteSelectedFilter = new DeleteSelectedFilters(this);
            DeleteAllFilters = new DeleteAllFilters(this);
        }

        public ObservableCollection<GeneralFilterOptionViewModel> PreElementsFilter { get; }
        public SnackbarMessageQueue SnackbarMessageQueue { get; }
        public ICommand? TakeSampleCommand { get; }
        public ICommand? DeleteSelectedFilter { get; }
        public ICommand? DeleteAllFilters { get; }
        public ICommand? ManualSelectionCommand { get; }
        public ICommand? NewSelectionCommand { get; }
        public ICommand? AddToSelectionCommand { get; }
        public ICommand? RemoveSelectionCommand { get; }

        [RelayCommand]
        public void ShowAboutDialog()
        {
            try
            {
                var dialog = new Window();
                dialog.Title = "A propos";
                dialog.Width = 512;
                dialog.Height = 512;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.SizeToContent = SizeToContent.WidthAndHeight;
                dialog.Content = new Views.AboutPage();
                dialog.ShowDialog();

            }
            catch
            {
                // Just catching no need to handle
            }
        }

        public ObservableCollection<ISelectionFilterViewModel> Filters { get; }
        private ObservableCollection<ISelectionFilterViewModel> selectedFilters;
        public ObservableCollection<ISelectionFilterViewModel> SelectedFilters
        {
            get => selectedFilters;
            set
            {
                if (selectedFilters != value) { selectedFilters = value; OnPropertyChanged(); }
            }
        }

        internal IEnumerable<ElementId> GetFiltererdElement(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            HashSet<ElementId> filtered = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElementIds().ToHashSet();
            foreach (var prefilter in PreElementsFilter)
            {
                if (!prefilter.IsActive) continue;
                var prefiltered = prefilter.Filter.GetFilteredElements(app);
                filtered = filtered.Intersect(prefiltered.Select(f => f.Id)).ToHashSet();
            }

            var elements = filtered.Select(f => doc.GetElement(f));
            foreach (var filter in Filters)
            {
                var temp = filter.PassFilter(elements);
                elements = temp;
            }

            return elements.Select(f => f.Id);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    takeSampleExternalEvent.Dispose();
                    pickByRectangleExternalEvent.Dispose();
                    Filters.Clear();
                    applyNewExternalEvent.Dispose();
                    applyAddExternalEvent.Dispose();
                    applyRemoveExternalEvent.Dispose();
                }
                disposedValue = true;
            }
        }
        ~SelectionOptionsViewModel() { Dispose(disposing: false); }
        public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }

        internal void Init(Document document)
        {
            var levels = new FilteredElementCollector(document).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Levels).OfClass(typeof(Level)).Cast<Level>().OrderBy(f => f.ProjectElevation).Select(f => new Tuple<ElementId, string>(f.Id, f.Name));
            var rooms = new FilteredElementCollector(document).WhereElementIsNotElementType().WherePasses(new RoomFilter()).ToElementIds();
            foreach (var item in PreElementsFilter) { item.Filter.Init(document); }
        }

        internal void SetNumberOfSelectedElements(uint numberOfSelectedElements)
        {
            string message = numberOfSelectedElements switch { 0 => "Aucun élément sélectionné", 1 => "1 élément sélectionné", _ => $"{numberOfSelectedElements} éléments sélectionnés" };
            SnackbarMessageQueue.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(3));
        }
    }
}
