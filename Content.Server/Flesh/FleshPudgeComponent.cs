using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Flesh
{
    [RegisterComponent]
    public sealed class FleshPudgeComponent : Component
    {
        [DataField("actionThrowWorm", required: true)]
        public WorldTargetAction ActionThrowWorm = new();

        [DataField("actionAcidSpit", required: true)]
        public WorldTargetAction ActionAcidSpit = new();

        [DataField("actionAbsorbBloodPool", required: true)]
        public InstantAction ActionAbsorbBloodPool = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("soundThrowWorm")]
        public SoundSpecifier? SoundThrowWorm = new SoundPathSpecifier("/Audio/Animals/Flesh/throw_worm.ogg");

        [ViewVariables(VVAccess.ReadWrite),
         DataField("wormMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string WormMobSpawnId = "MobFleshWorm";

        [ViewVariables(VVAccess.ReadWrite),
         DataField("bulletAcidSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BulletAcidSpawnId = "BulletSplashAcid";

        [DataField("healBloodAbsorbReagents")] public List<Solution.ReagentQuantity> HealBloodAbsorbReagents = new()
        {
            new Solution.ReagentQuantity("Omnizine", 1),
            new Solution.ReagentQuantity("DexalinPlus", 0.50),
            new Solution.ReagentQuantity("Iron", 0.50)
        };

        [DataField("bloodAbsorbSound")]
        public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
    }
}
