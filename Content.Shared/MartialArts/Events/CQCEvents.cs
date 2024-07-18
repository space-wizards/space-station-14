using Content.Shared.Movement.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.MartialArts;

[ImplicitDataDefinitionForInheritors]
public sealed partial class CQCSlamPerformedEvent : EntityEventArgs
{
}

[ImplicitDataDefinitionForInheritors]
public sealed partial class CQCKickPerformedEvent : EntityEventArgs
{
}

[ImplicitDataDefinitionForInheritors]
public sealed partial class CQCRestrainPerformedEvent : EntityEventArgs
{
}

[ImplicitDataDefinitionForInheritors]
public sealed partial class CQCPressurePerformedEvent : EntityEventArgs
{
}

[ImplicitDataDefinitionForInheritors]
public sealed partial class CQCConsecutivePerformedEvent : EntityEventArgs
{
}
