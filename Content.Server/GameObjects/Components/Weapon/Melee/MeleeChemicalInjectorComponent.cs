#nullable enable
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeChemicalInjectorComponent : Component
    {
        public override string Name => "MeleeChemicalInjector";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(1);

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }
        [DataField("transferEfficiency")]
        private float _transferEfficiency = 1f;

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case MeleeHitMessage meleeHit:
                    InjectEntities(meleeHit.HitEntities);
                    break;
            }
        }

        private void InjectEntities(List<IEntity> hitEntities)
        {
            if (!Owner.TryGetComponent<SolutionContainerComponent>(out var solutionContainer))
                return;

            var hitBloodstreams = new List<BloodstreamComponent>();
            foreach (var entity in hitEntities)
            {
                if (entity.Deleted)
                    continue;

                if (entity.TryGetComponent<BloodstreamComponent>(out var bloodstream))
                    hitBloodstreams.Add(bloodstream);
            }

            if (!hitBloodstreams.Any())
                return;

            var removedSolution = solutionContainer.Solution.SplitSolution(TransferAmount * hitBloodstreams.Count);
            var removedVol = removedSolution.TotalVolume;
            var solutionToInject = removedSolution.SplitSolution(removedVol * TransferEfficiency);
            var volPerBloodstream = solutionToInject.TotalVolume * (1 / hitBloodstreams.Count);

            foreach (var bloodstream in hitBloodstreams)
            {
                var individualInjection = solutionToInject.SplitSolution(volPerBloodstream);
                bloodstream.TryTransferSolution(individualInjection);
            }
        }
    }
}
