using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Specifies an explosion range should this entity be exploded.
    /// </summary>
    [RegisterComponent]
    public class ExplosiveComponent : Component
    {
        public override string Name => "Explosive";

        [DataField("devastationRange")]
        public int DevastationRange;
        [DataField("heavyImpactRange")]
        public int HeavyImpactRange;
        [DataField("lightImpactRange")]
        public int LightImpactRange;
        [DataField("flashRange")]
        public int FlashRange;

        public bool Exploding { get; set; } = false;
    }
}
