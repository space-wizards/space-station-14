using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public class FlammableReaction : ReagentEffect
    {
        public override void Metabolize(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out FlammableComponent? flammable)) return;

            EntitySystem.Get<FlammableSystem>().AdjustFireStacks(args.SolutionEntity, args.Metabolizing.Float() / 5f, flammable);
            args.Source?.RemoveReagent(args.Reagent.ID, args.Metabolizing);
        }
    }
}
