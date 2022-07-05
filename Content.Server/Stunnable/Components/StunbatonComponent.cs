using Content.Shared.Sound;
using Content.Shared.Timing;

namespace Content.Server.Stunnable.Components
{
    [RegisterComponent, Access(typeof(StunbatonSystem))]
    public sealed class StunbatonComponent : Component
    {
        public bool Activated = false;

        /// <summary>
        /// What the <see cref="UseDelayComponent"/> is when the stun baton is active.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("activeCooldown")]
        public TimeSpan ActiveDelay = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Store what the <see cref="UseDelayComponent"/> was before being activated.
        /// </summary>
        public TimeSpan? OldDelay;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energyPerUse")]
        public float EnergyPerUse { get; set; } = 350;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("onThrowStunChance")]
        public float OnThrowStunChance { get; set; } = 0.20f;

        [DataField("stunSound")]
        public SoundSpecifier StunSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");

        [DataField("sparksSound")]
        public SoundSpecifier SparksSound { get; set; } = new SoundCollectionSpecifier("sparks");

        [DataField("turnOnFailSound")]
        public SoundSpecifier TurnOnFailSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}
