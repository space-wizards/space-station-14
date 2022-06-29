using Content.Shared.Sound;
using Content.Shared.Timing;

namespace Content.Server.Stunnable.Components
{
    [RegisterComponent, Access(typeof(StunbatonSystem))]
    public sealed class StunbatonComponent : Component
    {
        public bool Activated = false;

        /// <summary>
        /// What the stun cooldown is when the stun baton is active.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("activeCooldown")]
        public TimeSpan ActiveDelay = TimeSpan.FromSeconds(4);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 1.5f;

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
