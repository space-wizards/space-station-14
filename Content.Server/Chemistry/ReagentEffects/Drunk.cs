using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Drunk;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects
{
    public class Drunk : ReagentEffect
    {
        /// <summary>
        ///     BoozePower is how long each metabolism cycle will make the drunk effect last for.
        /// </summary>
        [DataField("boozePower")]
        public float BoozePower = 2f;

        public override void Effect(ReagentEffectArgs args)
        {
            var drunkSys = EntitySystem.Get<SharedDrunkSystem>();
            drunkSys.TryApplyDrunkenness(args.SolutionEntity, BoozePower);
        }
    }
}
