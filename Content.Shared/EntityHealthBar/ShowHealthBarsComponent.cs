using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.EntityHealthBar
{
    /// <summary>
    /// This component allows you to see health bars above damageable mobs.
    /// </summary>
    [RegisterComponent]
    public sealed class ShowHealthBarsComponent : Component
    {
        /// <summary>
        /// If null, displays all health bars.
        /// If not null, displays health bars of only that damage container.
        /// </summary>

        [DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
        public List<string> DamageContainers = new();
    }
}
