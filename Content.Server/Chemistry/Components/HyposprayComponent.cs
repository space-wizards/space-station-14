using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class HyposprayComponent : SharedHyposprayComponent
    {
        // TODO: This should be on clumsycomponent.
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ClumsyFailChance = 0.5f;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount = FixedPoint2.New(5);

        [DataField]
        public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

        /// <summary>
        /// Whether or not the hypo is able to inject only into mobs. On false you can inject into beakers/jugs
        /// </summary>
        [DataField]
        public bool OnlyMobs = true;

        /// <summary>
        /// If true, hypo will initiate a DoAfter before injecting
        /// </summary>

        [DataField]
        public bool HasInjectionDelay = false;

        [DataField]
        public float BaseDelay = 4f;

        [DataField]
        public float CritDelay = 1f;

        [DataField]
        public float CombatDelay = 8f;
    }
}
