using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using SelectionSet.Helpers;

namespace SelectionSet.ViewModels
{
    internal enum SelectionHandlerAction { Apply, Add, Remove }

    internal partial class CheckableObject<T> : ObservableObject
    {
        public T Data { get; }
        public CheckableObject(T data) { Data = data; }
        [ObservableProperty]
        private bool isChecked;
    }

    internal interface ISelectionFilterViewModel
    {
        string Label { get; }
        bool PassFilter(Element element);
        IEnumerable<Element> PassFilter(IEnumerable<Element> elements);
    }

    internal class ParameterSelectionFilterViewModel : ISelectionFilterViewModel
    {
        public ParameterSelectionFilterViewModel(Parameter parameter, bool isFromType = false)
        {
            Label = parameter.Definition != null ?
            parameter.Definition.Name :
            SelectionSet.Properties.Resources.LabelUnknowParameter;

            Identity = ParameterIdentity.FromParameter(parameter, isFromType);
            IsShared = parameter.IsShared;
            Guid = parameter.IsShared ? parameter.GUID : null;
            BuiltInParameter = GetBuiltInParameter(parameter);
            StorageType = parameter.StorageType;
            ValueAsString = parameter.AsValueString();
            ValueAsString = ValueAsString != null ? ValueAsString.Trim() : ValueAsString;

            ShortValue = CreateShortValue(ValueAsString);
            FriendlyStorageType = GetFriendlyStorageType(StorageType);
            FriendlyOwnerScope = Identity.OwnerScope;

            TechnicalDetails = BuildTechnicalDetails();
        }

        public string Label { get; private set; }
        public bool IsShared { get; private set; }
        public Guid? Guid { get; private set; }
        public BuiltInParameter? BuiltInParameter { get; private set; }
        public StorageType StorageType { get; private set; }
        public string? ValueAsString { get; private set; }
        public string TechnicalDetails { get; }

        public ParameterIdentity Identity { get; }
        public string ShortValue { get; }
        public string FriendlyStorageType { get; }
        public string FriendlyOwnerScope { get; }

        private static string CreateShortValue(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var v = value.Replace("\n", " ").Replace("\r", " ").Trim();
            if (v.Length <=24) return v;
            return v.Substring(0,21) + "...";
        }

        private static string GetFriendlyStorageType(StorageType st)
        {
            return st switch
            {
                StorageType.String => SelectionSet.Properties.Resources.LabelFriendlyStorageTypeText,
                StorageType.Double => SelectionSet.Properties.Resources.LabelFriendlyStorageTypeNumber,
                StorageType.Integer => SelectionSet.Properties.Resources.LabelFriendlyStorageTypeInteger,
                StorageType.ElementId => SelectionSet.Properties.Resources.LabelFriendlyStorageTypeElement,
                StorageType.None => SelectionSet.Properties.Resources.LabelFriendlyStorageTypeNone,
                _ => st.ToString()
            };
        }

        private static string NormalizeValue(string? v)
        {
            if (v == null) return string.Empty;
            return string.Join(" ", v.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public bool Pass(Parameter p, bool isFromType = false)
        {
            try
            {
                var candidate = ParameterIdentity.FromParameter(p, isFromType);
                if (!Identity.Equals(candidate))
                    return false;

                string? valueAsString = p.AsValueString();
                valueAsString = valueAsString != null ? valueAsString.Trim() : valueAsString;

                var a = NormalizeValue(ValueAsString);
                var b = NormalizeValue(valueAsString);

                return string.Equals(a, b, StringComparison.Ordinal);
            }
            catch
            {
                // Just catch it
            }
            return false;
        }

        public bool PassFilter(Element element)
        {
            Document doc = element.Document;
            var elementType = doc.GetElement(element.GetTypeId());
            // If element is a type (ElementType), mark parameters as from type
            bool isElementType = element is ElementType;

            if (elementType != null && PassFilter(elementType))
                return true;

            foreach (Parameter p in element.Parameters)
            {
                if (Pass(p, isElementType))
                    return true;
            }

            return false;
        }

        public IEnumerable<Element> PassFilter(IEnumerable<Element> elements)
        {
            Dictionary<ElementId, bool> memory = new Dictionary<ElementId, bool>();
            List<Element> passing = new List<Element>();
            foreach (var el in elements)
            {
                if (memory.ContainsKey(el.Id))
                {
                    if (memory[el.Id])
                        passing.Add(el);
                    continue;
                }

                var hasPassing = PassFilter(el);
                if (hasPassing) { passing.Add(el); }
                memory[el.Id] = hasPassing;
            }
            return passing;
        }

        private string BuildTechnicalDetails()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Name: {Label}");
            sb.AppendLine($"Owner: {FriendlyOwnerScope}");
            sb.AppendLine($"Storage: {FriendlyStorageType}");
            if (IsShared && Guid.HasValue)
            {
                sb.AppendLine($"Shared GUID: {Guid}");
            }
            else if (BuiltInParameter.HasValue)
            {
                sb.AppendLine($"Built-in id: {BuiltInParameter.Value}");
            }
            return sb.ToString();
        }

        private static BuiltInParameter? GetBuiltInParameter(Parameter parameter)
        {
#if NET8_0_OR_GREATER
            return (parameter.IsBuiltInParameter() && parameter.Definition is InternalDefinition internalDefinition) ?
            internalDefinition.BuiltInParameter : null;
#else
 return (parameter.Definition is InternalDefinition internalDefinition) ?
 internalDefinition.BuiltInParameter : null;
#endif
        }
    }

    internal class CustomnSelectionFilterByApp : ISelectionFilter
    {
        private readonly List<ISelectionFilterViewModel> filters;
        private readonly Document document;
        private readonly HashSet<ElementId> elementIds;

        public CustomnSelectionFilterByApp(Document document, IEnumerable<ISelectionFilterViewModel> filters)
        {
            this.filters = new List<ISelectionFilterViewModel>(filters);
            this.document = document;
            elementIds = new HashSet<ElementId>();
        }

        public bool AllowElement(Element elem)
        {
            foreach (var filter in filters)
            {
                if (filter.PassFilter(elem))
                {
                    elementIds.Add(elem.Id);
                    return true;
                }
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            var element = document.GetElement(reference);
            return AllowElement(element);
        }
    }

}
