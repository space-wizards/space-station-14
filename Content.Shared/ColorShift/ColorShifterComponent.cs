using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ColorShift;

/// <summary>
/// This is used for tracking entities with the hueshifting ability
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ColorShifterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Action = "ActionOpenColorShift";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public TimeSpan HueShiftLength = TimeSpan.FromSeconds(5);

    [Serializable, NetSerializable]
    public enum ColorShiftUiKey
    {
        Key,
    }
}

/// <summary>
/// Used to open the hue shift gui thru an action
/// </summary>
public sealed partial class OpenColorShiftEvent : InstantActionEvent
{
}
