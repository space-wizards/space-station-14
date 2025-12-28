using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial record StationRecord;
