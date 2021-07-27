using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;


namespace Content.Shared.Explosion.Components
{
    public abstract class SharedExplosiveComponent : Component
    {
        public override string Name => "Explosive";

        /// <summary>
        /// Energy content of explosive charge. Roughly correlates to how far the shockwave will get before running out of steam. Depleted as explosion expands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energy")]
        public int Energy { get; set; } = 10;

        /// <summary>
        /// Brisance, or "shattering" effect of explosive. Contributes to the amount of damage a shockwave deals as it passes/is stopped by an object.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("brisance")]
        public int Brisance { get; set; } = 10;

        public bool Exploding { get; set; } = false;
    }
}
