using JetBrains.Annotations;
using Content.Shared.Disease;
using Content.Server.Medical;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Forces you to poop.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseasePoop : DiseaseEffect
    {
        /// How many units of thirst to add each time we poop
        [DataField("thirstAmount")]
        public float ThirstAmount = -30f;
        /// How many units of hunger to add each time we poop
        [DataField("hungerAmount")]
        public float HungerAmount = -50f;

        public override void Effect(DiseaseEffectArgs args)
        {
            var poopSys = args.EntityManager.EntitySysManager.GetEntitySystem<PoopSystem>();

            poopSys.Poop(args.DiseasedEntity, ThirstAmount, HungerAmount);
        }
    }
}
