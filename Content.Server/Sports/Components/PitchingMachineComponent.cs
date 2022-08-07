using Robust.Shared.Audio;

namespace Content.Server.Sports.Components
{
    /// <summary>
    /// Component for sports ball launchers, when enabled they launch their inventory of sports balls
    /// </summary>
    [RegisterComponent]
    public sealed class PitchingMachineComponent : Component
    {
        [ViewVariables] public bool IsOn = false;

        [DataField("shootDistanceMin")]
        [ViewVariables]
        public float ShootDistanceMin = 7f;

        [DataField("shootDistanceMax")]
        [ViewVariables]
        public float ShootDistanceMax = 12f;

        /// <summary>
        /// The amount of time between each shooting attempt
        /// </summary>
        [DataField("shootSpeed")]
        [ViewVariables]
        public float ShootCooldown = 10f;

        [DataField("accumulatedFrametime")] public float AccumulatedFrametime;

        [ViewVariables(VVAccess.ReadWrite)] public float CurrentLauncherCooldown;

        [DataField("fireSound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");
    }
}
