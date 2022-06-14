using Content.Shared.Actions.ActionTypes;
using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.RatKing
{
    [RegisterComponent]
    public sealed class RatKingComponent : Component
    {
        /// <summary>
        ///     The action for the Raise Army ability
        /// </summary>
        [DataField("actionRaiseArmy", required: true)]
        public InstantAction ActionRaiseArmy = new();

        /// <summary>
        ///     The amount of hunger one use of Raise Army consumes
        /// </summary>
        [DataField("hungerPerArmyUse", required: true)]
        public float HungerPerArmyUse = 25f;

        /// <summary>
        ///     The entity prototype of the mob that Raise Army summons
        /// </summary>
        [DataField("armyMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ArmyMobSpawnId = "MobRatServant";

        /// <summary>
        ///     The action for the Domain ability
        /// </summary>
        [DataField("actionDomain", required: true)]
        public InstantAction ActionDomain = new();

        /// <summary>
        ///     The amount of hunger one use of Domain consumes
        /// </summary>
        [DataField("hungerPerDomainUse", required: true)]
        public float HungerPerDomainUse = 50f;

        /// <summary>
        ///     The disease prototype id that the Domain ability spreads
        /// </summary>
        [DataField("domainDiseaseId", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
        public string DomainDiseaseId = "Plague";

        /// <summary>
        ///     The range of the Domain ability.
        /// </summary>
        [DataField("domainRange")]
        public float DomainRange = 3f;

    }
};
