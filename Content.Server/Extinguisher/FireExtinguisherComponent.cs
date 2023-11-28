using Content.Shared.Extinguisher;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Extinguisher;

[NetworkedComponent, RegisterComponent]
[Access(typeof(FireExtinguisherSystem))]
public sealed partial class FireExtinguisherComponent : SharedFireExtinguisherComponent
{
    /// <summary>
    /// "ActionToggleSafety" refers to an entity created in Resources/Prototypes/Actions/extinguisher.yml
    /// This configures the visual appearance of the button, including text description & sprites.
    /// </summary>
    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleSafety";

    [DataField("toggleActionEntity")] public EntityUid? ToggleActionEntity;
}
