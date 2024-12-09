using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Gas masks or the likes; used by <see cref="InternalsComponent"/> for breathing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[ComponentProtoName("BreathMask")]
public sealed partial class BreathToolComponent : Component
{
    /// <summary>
    /// Tool is functional only in allowed slots
    /// </summary>
    [DataField]
    public SlotFlags AllowedSlots = SlotFlags.MASK | SlotFlags.HEAD;

    [ViewVariables]
    public bool IsFunctional => ConnectedInternalsEntity != null;

    /// <summary>
    /// Entity that the breath tool is currently connected to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedInternalsEntity;
}
