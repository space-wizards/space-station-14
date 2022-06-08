using Content.Shared.Actions.ActionTypes;

namespace Content.Server.RatKing
{
    [RegisterComponent]
    public sealed class RatKingComponent: Component
    {
        [DataField("actionRaiseArmy", required: true)]
        public InstantAction ActionRaiseArmy = new();

        [DataField("hungerPerArmyUse", required: true)]
        public float HungerPerArmyUse = 25f;

        public string ArmyMobSpawnId = "MobRatServant";

        [DataField("actionDomain", required: true)]
        public InstantAction ActionDomain = new();

        [DataField("hungerPerDomainUse", required: true)]
        public float HungerPerDomainUse = 50f;

        public string DomainDiseaseId = "Plague";

        public float DomainRange = 3f;

    }
};
