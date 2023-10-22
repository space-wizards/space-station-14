using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Adds StealthComponent to the user when enabled, either by an action or the system's SetEnabled method.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(StealthClothingSystem))]
public sealed partial class StealthClothingComponent : Component
{
    /// <summary>
    /// Whether stealth effect is enabled.
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Number added to MinVisibility when stealthed, to make the user not fully invisible.
    /// </summary>
    [DataField("visibility"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Visibility;

    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionTogglePhaseCloak";

    /// <summary>
    /// The action for enabling and disabling stealth.
    /// </summary>
    [DataField, AutoNetworkedField] public EntityUid? ToggleActionEntity;
}

/// <summary>
/// When stealth is enabled, disables it.
/// When it is disabled, raises <see cref="AttemptStealthEvent"/> before enabling.
/// Put any checks in a handler for that event to cancel it.
/// </summary>
public sealed partial class ToggleStealthEvent : InstantActionEvent
{
}
