using Content.Shared.Damage.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Overlays;

/// <summary>
/// This component allows you to see health bars above damageable mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowHealthBarsComponent : Component
{
    /// <summary>
    /// Displays health bars of the damage containers.
    /// </summary>
    [DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
    public List<string> DamageContainers = new();

    [DataField]
    public ProtoId<HealthIconPrototype>? HealthStatusIcon = "HealthIconFine";
}
