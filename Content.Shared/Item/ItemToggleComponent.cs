using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Weapons.Melee.ItemToggle;

[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleComponent : Component
{
    public bool Activated = false;

    [DataField("activateSound")]
    public SoundSpecifier ActivateSound { get; set; } = default!;

    [DataField("deActivateSound")]
    public SoundSpecifier DeActivateSound { get; set; } = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedDisarmMalus")]
    public float ActivatedDisarmMalus = 0.6f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offSize")]
    public int OffSize = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("onSize")]
    public int OnSize = 9999;
}

[ByRefEvent]
public readonly record struct ItemToggleActivatedEvent();

[ByRefEvent]
public readonly record struct ItemToggleDeactivatedEvent();
