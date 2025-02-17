using Robust.Shared.Containers;

namespace Content.Server.DeadSpace.Abilities.Cocoon.Components;

[RegisterComponent]
[Access(typeof(CocoonSystem))]
public sealed partial class CocoonComponent : Component
{
    public Container Stomach = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsHermetically = true;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Prisoner;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Pressure = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Mute = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Pacified = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Blindable = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTick;
}

[ByRefEvent]
public readonly record struct InsertIntoCocoonEvent(EntityUid Target);
