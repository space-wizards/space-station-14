using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

[NetSerializable, Serializable]
public sealed class EnterMappingMode : EntityEventArgs
{
}


[NetSerializable, Serializable]
public sealed class ExitMappingMode : EntityEventArgs
{
}
