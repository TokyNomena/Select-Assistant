using Autodesk.Revit.DB;
using System;

namespace SelectionSet.ViewModels {
 internal sealed class ParameterIdentity : IEquatable<ParameterIdentity> {
 public string Name { get; }
 public bool IsShared { get; }
 public Guid? SharedGuid { get; }
 public BuiltInParameter? BuiltInParam { get; }
 public StorageType StorageType { get; }
 public string OwnerScope { get; } // "Instance" | "Type"

 public ParameterIdentity(string name, bool isShared, Guid? sharedGuid, BuiltInParameter? builtInParam, StorageType storageType, string ownerScope) {
 Name = name ?? throw new ArgumentNullException(nameof(name));
 IsShared = isShared;
 SharedGuid = sharedGuid;
 BuiltInParam = builtInParam;
 StorageType = storageType;
 OwnerScope = ownerScope;
 }

 public static ParameterIdentity FromParameter(Parameter p, bool isFromType = false) {
 Guid? guid = null;
 try {
 guid = p.IsShared ? p.GUID : (Guid?)null;
 } catch {
 guid = null;
 }
#if NET8_0_OR_GREATER
 BuiltInParameter? bip = null;
 try {
 bip = (p.IsBuiltInParameter() && p.Definition is InternalDefinition id) ? id.BuiltInParameter : null;
 } catch {
 bip = null;
 }
#else
 BuiltInParameter? bip = (p.Definition is InternalDefinition id2) ? id2.BuiltInParameter : null;
#endif
 string owner = isFromType ? "Type" : "Instance";
 var name = string.Empty;
 try { name = p.Definition?.Name ?? string.Empty; } catch { name = string.Empty; }
 return new ParameterIdentity(name, p.IsShared, guid, bip, p.StorageType, owner);
 }

 public bool Equals(ParameterIdentity? other) {
 if (other == null) return false;
 if (IsShared && other.IsShared) {
 if (SharedGuid.HasValue && other.SharedGuid.HasValue)
 return SharedGuid.Value == other.SharedGuid.Value;
 return false;
 }
 if (BuiltInParam.HasValue && other.BuiltInParam.HasValue) {
 return BuiltInParam.Value == other.BuiltInParam.Value;
 }
 return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
 StorageType == other.StorageType &&
 string.Equals(OwnerScope, other.OwnerScope, StringComparison.Ordinal);
 }

 public override bool Equals(object? obj) => Equals(obj as ParameterIdentity);

 public override int GetHashCode() {
 if (IsShared && SharedGuid.HasValue) return SharedGuid.GetHashCode();
 if (BuiltInParam.HasValue) return BuiltInParam.Value.GetHashCode();
 unchecked {
 int hash =17;
 hash = hash *23 + Name.GetHashCode(StringComparison.Ordinal);
 hash = hash *23 + StorageType.GetHashCode();
 hash = hash *23 + (OwnerScope?.GetHashCode(StringComparison.Ordinal) ??0);
 return hash;
 }
 }
 }
}
