using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SelectionSet.ViewModels;


namespace SelectionSet.Behaviors
{
    public static class ListBoxBehavior
    {
        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(ListBoxBehavior),
                new PropertyMetadata(false));

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(INotifyCollectionChanged),
                typeof(ListBoxBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static INotifyCollectionChanged GetSelectedItems(DependencyObject obj)
        {
            return (INotifyCollectionChanged)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, INotifyCollectionChanged value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        private static bool GetIsUpdating(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject obj, bool value)
        {
            obj.SetValue(IsUpdatingProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBox = d as ListBox;
            if (listBox == null) return;

            // Unsubscribe from old collection
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= OnViewModelCollectionChanged;
            }

            listBox.SelectionChanged -= OnSelectionChanged;

            // Update ListBox from ViewModel
            if (e.NewValue is ICollection newCollection)
            {
                SetIsUpdating(listBox, true);
                listBox.SelectedItems.Clear();
                foreach (var item in newCollection)
                {
                    listBox.SelectedItems.Add(item);
                }
                SetIsUpdating(listBox, false);

                // Subscribe to collection changes
                if (e.NewValue is INotifyCollectionChanged newNotifyCollection)
                {
                    newNotifyCollection.CollectionChanged += OnViewModelCollectionChanged;
                }
            }

            listBox.SelectionChanged += OnSelectionChanged;
        }

        private static void OnViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Find the ListBox associated with this collection
            // This is called when the ViewModel collection changes
            if (sender is not INotifyCollectionChanged collection) return;

            // We need to find which ListBox is bound to this collection
            // Store a weak reference in the collection changed handler
            var listBox = GetListBoxForCollection(collection);
            if (listBox == null) return;

            if (GetIsUpdating(listBox)) return;

            SetIsUpdating(listBox, true);

            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            foreach (var item in e.NewItems)
                            {
                                if (!listBox.SelectedItems.Contains(item))
                                {
                                    listBox.SelectedItems.Add(item);
                                }
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (var item in e.OldItems)
                            {
                                listBox.SelectedItems.Remove(item);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        listBox.SelectedItems.Clear();
                        if (sender is ICollection items)
                        {
                            foreach (var item in items)
                            {
                                listBox.SelectedItems.Add(item);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldItems != null)
                        {
                            foreach (var item in e.OldItems)
                            {
                                listBox.SelectedItems.Remove(item);
                            }
                        }
                        if (e.NewItems != null)
                        {
                            foreach (var item in e.NewItems)
                            {
                                listBox.SelectedItems.Add(item);
                            }
                        }
                        break;
                }
            }
            finally
            {
                SetIsUpdating(listBox, false);
            }
        }

        private static readonly ConditionalWeakTable<INotifyCollectionChanged, ListBox> CollectionToListBoxMap =
            new ConditionalWeakTable<INotifyCollectionChanged, ListBox>();

        private static ListBox? GetListBoxForCollection(INotifyCollectionChanged collection)
        {
            CollectionToListBoxMap.TryGetValue(collection, out var listBox);
            return listBox;
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            if (GetIsUpdating(listBox)) return;

            var selectedItems = GetSelectedItems(listBox) as IList;
            if (selectedItems == null) return;

            SetIsUpdating(listBox, true);

            try
            {
                // Remove old items
                foreach (var item in e.RemovedItems)
                {
                    selectedItems.Remove(item);
                }

                // Add new items
                foreach (var item in e.AddedItems)
                {
                    if (!selectedItems.Contains(item))
                    {
                        selectedItems.Add(item);
                    }
                }
            }
            finally
            {
                SetIsUpdating(listBox, false);
            }

            // Map this collection to this ListBox for reverse lookup
            if (selectedItems is INotifyCollectionChanged collection)
            {
                CollectionToListBoxMap.Remove(collection);
                CollectionToListBoxMap.Add(collection, listBox);
            }
        }
    }
}

namespace SelectionSet.DataTemplates
{
    internal class SelectionFilterDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ParameterSelectionFilter { get; set; }
        public DataTemplate? Unknow { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is ISelectionFilterViewModel model))
                return base.SelectTemplate(item, container);

            if (model is ParameterSelectionFilterViewModel parameter)
            {
                if (ParameterSelectionFilter == null)
                    return base.SelectTemplate(item, container);
                return ParameterSelectionFilter;
            }
            return base.SelectTemplate(item, container);
        }
    }

    internal class GeneralFilterDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ByCategoryGeneralFilter { get; set; }
        public DataTemplate? VisibleOnlyOnActiveViewGeneralFilter { get; set; }
        public DataTemplate? OnLevelGeneralFilter { get; set; }
        public DataTemplate? ToRoomSpaceGeneralFilter { get; set; }
        public DataTemplate? FromRoomSpaceGeneralFilter { get; set; }
        public DataTemplate? SpatialCollisionRoomSpaceGeneralFilter { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is IGeneralFilter model))
                return base.SelectTemplate(item, container);

            foreach (var dataTemplate in new List<DataTemplate?>() {
                ByCategoryGeneralFilter,
                VisibleOnlyOnActiveViewGeneralFilter,
                OnLevelGeneralFilter,
                ToRoomSpaceGeneralFilter,
                FromRoomSpaceGeneralFilter,
                SpatialCollisionRoomSpaceGeneralFilter
            })
            {
                if (dataTemplate == null)
                    continue;

                if (dataTemplate.DataType as Type == model.GetType())
                    return dataTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}

namespace SelectionSet.Views
{
    public partial class SelectionOptionsView : UserControl, IDisposable
    {
        private readonly SelectionOptionsViewModel model;
        private bool disposedValue;

        public SelectionOptionsView()
        {
            InitializeComponent();

            model = new SelectionOptionsViewModel();
            DataContext = model;
        }

        public void Init(Document document)
        {
            model.Init(document);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    model.Dispose();
                }
                disposedValue = true;
            }
        }

        ~SelectionOptionsView()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
