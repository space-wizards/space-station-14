using System.Collections.Generic;
using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Metabolism
{
    // TODO mirror in the future working on mechanisms move updating here to BodySystem so it can be ordered?
    [UsedImplicitly]
    public class MetabolizerSystem : EntitySystem
    {
        [Dependency]
        private readonly SolutionContainerSystem _solutionContainerSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnMetabolizerInit);
        }

        private void OnMetabolizerInit(EntityUid uid, MetabolizerComponent component, ComponentInit args)
        {
            _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var metab in EntityManager.EntityQuery<MetabolizerComponent>(false))
            {
                metab.AccumulatedFrametime += frameTime;

                // Only update as frequently as it should
                if (metab.AccumulatedFrametime >= metab.UpdateFrequency)
                {
                    metab.AccumulatedFrametime = 0.0f;
                    TryMetabolize(metab);
                }
            }
        }

        private void TryMetabolize(MetabolizerComponent comp)
        {
            // todo rewrite
        }
    }
}
