using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ChemicalInjectionProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "ChemicalInjectionProjectile";

        [ViewVariables]
        private SolutionContainerComponent _solutionContainer;

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }
        private float _transferEfficiency;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.TransferAmount, "transferAmount", ReagentUnit.New(1));
            serializer.DataField(ref _transferEfficiency, "transferEfficiency", 1f);
        }

        public override void Initialize()
        {
            base.Initialize();
            _solutionContainer = Owner.EnsureComponent<SolutionContainerComponent>();
        }

        void ICollideBehavior.CollideWith(IPhysBody ourBody, IPhysBody otherBody)
        {
            if (!otherBody.Entity.TryGetComponent<BloodstreamComponent>(out var bloodstream))
                return;

            var solution = _solutionContainer.Solution;
            var solRemoved = solution.SplitSolution(TransferAmount);
            var solRemovedVol = solRemoved.TotalVolume;

            var solToInject = solRemoved.SplitSolution(solRemovedVol * TransferEfficiency);

            bloodstream.TryTransferSolution(solToInject);
        }
    }
}
