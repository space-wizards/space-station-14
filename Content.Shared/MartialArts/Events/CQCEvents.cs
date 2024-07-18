using Content.Shared.Movement.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.MartialArts;

[Serializable, NetSerializable]
public sealed class CQCSlamPerformedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class CQCKickPerformedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class CQCRestrainPerformedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class CQCPressurePerformedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class CQCConsecutivePerformedEvent : EntityEventArgs
{
}
