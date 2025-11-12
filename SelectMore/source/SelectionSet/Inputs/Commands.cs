using System.Windows.Input;
using SelectionSet.Adapters;
using SelectionSet.ViewModels;

namespace SelectionSet.Inputs
{
    internal class ExternalEventUiCommand : ICommand
    {
        private readonly WeakReference<IExternalEventWrapper> externalEvent;

        public ExternalEventUiCommand(IExternalEventWrapper externalEvent)
        {
            this.externalEvent = new WeakReference<IExternalEventWrapper>(externalEvent);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (externalEvent.TryGetTarget(out var target) && target != null)
                {
                    target.Raise();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    internal class ActionUiCommand : ICommand
    {
        private readonly Func<object?, bool> canExecute;
        private readonly Action<object?> execute;

        public ActionUiCommand(Func<object?, bool> canExecute, Action<object?> execute)
        {
            this.canExecute = canExecute;
            this.execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }

    internal class DeleteSelectedFilters : ICommand
    {
        private readonly WeakReference<SelectionOptionsViewModel> model;

        public DeleteSelectedFilters(SelectionOptionsViewModel model)
        {
            this.model = new WeakReference<SelectionOptionsViewModel>(model);
            model.SelectedFilters.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler((o, args) =>
            {
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (model.TryGetTarget(out var target) && target != null)
            {
                return target.SelectedFilters != null && target.SelectedFilters.Count() > 0;
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (model.TryGetTarget(out var target) && target != null)
                {
                    if (!target.SelectedFilters.Any() || !target.Filters.Any())
                        return;

                    var filtersToRemove = new List<ISelectionFilterViewModel>(target.SelectedFilters);
                    var index = target.SelectedFilters.Select(f => target.Filters.IndexOf(f)).Where(f => f != -1).Min();
                    foreach (var filter in filtersToRemove)
                    {
                        target.Filters.Remove(filter);
                    }
                    if (index != -1 && target.Filters.Count > index)
                        target.SelectedFilters.Add(target.Filters[index]);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    internal class DeleteAllFilters : ICommand
    {
        private readonly WeakReference<SelectionOptionsViewModel> model;

        public DeleteAllFilters(SelectionOptionsViewModel model)
        {
            this.model = new WeakReference<SelectionOptionsViewModel>(model);
            model.Filters.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler((o, args) =>
            {
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (model.TryGetTarget(out var target) && target != null)
            {
                return target.Filters != null && target.Filters.Count() > 0;
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (model.TryGetTarget(out var target) && target != null)
                {
                    var filtersToRemove = new List<ISelectionFilterViewModel>(target.Filters);
                    foreach (var filter in filtersToRemove)
                    {
                        target.Filters.Remove(filter);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                ex.Message, "Selection Assistant",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
