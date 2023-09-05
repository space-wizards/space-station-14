using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Overlays;

/// <summary>
/// This component allows you to see health status icons above damageable mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowHealthIconsComponent : Component
{
    /// <summary>
    /// Displays health status icons of the damage containers.
    /// </summary>
    [DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
    public List<string> DamageContainers = new();
}
