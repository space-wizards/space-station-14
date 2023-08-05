using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an esword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed class ItemToggleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("activated"), AutoNetworkedField]
    public bool Activated;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundActivate"), AutoNetworkedField]
    public SoundSpecifier? ActivateSound;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundDeactivate"), AutoNetworkedField]
    public SoundSpecifier? DeactivateSound;
}

[ByRefEvent]
public readonly record struct ItemToggleActivateAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public readonly record struct ItemToggleActivatedEvent;

[ByRefEvent]
public readonly record struct ItemToggleDeactivateAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public readonly record struct ItemToggleDeactivatedEvent;
