using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Light.Component;

/// <summary>
///     Give mob ability to see in complete darkness.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class NightVisionComponent : Robust.Shared.GameObjects.Component
{
    [DataField("action")]
    public InstantAction Action = new()
    {
        Name = "action-name-night-vision",
        Description = "action-description-night-vision",
        Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/night-vision-off.png")),
        IconOn = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/night-vision.png")),
        CheckCanInteract = false,
        Event = new NightVisionToggleEvent()
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsEnabled = false;
}

[Serializable, NetSerializable]
public sealed class NightVisionComponentState : ComponentState
{
    public readonly bool IsEnabled;

    public NightVisionComponentState(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

/// <summary>
///     Raised when night vision mode was activated/deactivated.
/// </summary>
public sealed class NightVisionToggleEvent : InstantActionEvent
{

}
