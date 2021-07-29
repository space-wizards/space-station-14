using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class SolutionInjectOnCollideSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SolutionInjectOnCollideComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<SolutionInjectOnCollideComponent, StartCollideEvent>(HandleInjection);
        }

        private void HandleInit(EntityUid uid, SolutionInjectOnCollideComponent component, ComponentInit args)
        {
            component.Owner.EnsureComponentWarn<SolutionContainerComponent>($"{nameof(SolutionInjectOnCollideComponent)} requires a SolutionContainer on {component.Owner}!");
        }

        private void HandleInjection(EntityUid uid, SolutionInjectOnCollideComponent component, StartCollideEvent args)
        {
            if (!args.OtherFixture.Body.Owner.TryGetComponent<BloodstreamComponent>(out var bloodstream) ||
                !ComponentManager.TryGetComponent(uid, out SolutionContainerComponent? solutionContainer)) return;

            var solution = solutionContainer.Solution;
            var solRemoved = solution.SplitSolution(component.TransferAmount);
            var solRemovedVol = solRemoved.TotalVolume;

            var solToInject = solRemoved.SplitSolution(solRemovedVol * component.TransferEfficiency);

            bloodstream.TryTransferSolution(solToInject);
        }
    }
}
