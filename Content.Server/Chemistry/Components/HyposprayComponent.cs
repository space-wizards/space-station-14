using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class HyposprayComponent : SharedHyposprayComponent
    {
        // TODO: This should be on clumsycomponent.
        [DataField("clumsyFailChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ClumsyFailChance = 0.5f;

        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount = FixedPoint2.New(5);

        [DataField("injectSound")]
        public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

        [DataField]
        public float BaseDelay = 3f;
        [DataField]
        public float DelayPerUnit = 0.1f;
        [DataField]
        public float CritDelayReduction = 3f;
        [DataField]
        public float CombatDelayPenalty = 3f;

        /// <summary>
        /// Whether or not the hypo is able to inject only into mobs. On false you can inject into beakers/jugs
        /// </summary>
        [DataField("onlyMobs")]
        public bool OnlyMobs = true;
    }
}
