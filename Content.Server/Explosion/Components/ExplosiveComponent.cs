using Content.Server.Destructible.Thresholds.Behaviors;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    ///     Specifies an explosion range should this entity be exploded.
    /// </summary>
    /// <remarks>
    ///     Explosions can be caused by:
    ///     <list type="bullet">
    ///         <item>Reaching a damage threshold that causes a <see cref="ExplodeBehavior"/></item>
    ///         <item>Being triggered via the <see cref="ExplodeOnTriggerComponent"/></item>
    ///         <item>Manually by some other system via functions in <see cref="ExplosionHelper"/> (for example, chemistry's
    ///         <see cref="ExplosionReactionEffect"/>).</item>
    ///     </list>
    /// </remarks>
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
