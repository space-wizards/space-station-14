using System;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ChemicalInjectionProjectileComponent : Component, IStartCollide
    {
        public override string Name => "ChemicalInjectionProjectile";

        [ViewVariables]
        private SolutionContainerComponent _solutionContainer = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(1);

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }
        [DataField("transferEfficiency")]
        private float _transferEfficiency = 1f;

        public override void Initialize()
        {
            base.Initialize();
            _solutionContainer = Owner.EnsureComponent<SolutionContainerComponent>();
        }

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (!otherFixture.Body.Owner.TryGetComponent<BloodstreamComponent>(out var bloodstream))
                return;

            var solution = _solutionContainer.Solution;
            var solRemoved = solution.SplitSolution(TransferAmount);
            var solRemovedVol = solRemoved.TotalVolume;

            var solToInject = solRemoved.SplitSolution(solRemovedVol * TransferEfficiency);

            bloodstream.TryTransferSolution(solToInject);
        }
    }
}
