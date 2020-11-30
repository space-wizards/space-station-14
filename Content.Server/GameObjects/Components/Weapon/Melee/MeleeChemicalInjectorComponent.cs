using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeChemicalInjectorComponent : Component
    {
        public override string Name => "MeleeChemicalInjector";

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }
        private float _transferEfficiency;

        [ViewVariables]
        private SolutionContainerComponent _solutionContainer;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.TransferAmount, "transferAmount", ReagentUnit.New(1));
            serializer.DataField(ref _transferEfficiency, "transferEfficiency", 1f);
        }

        public override void Initialize()
        {
            base.Initialize();
            var meleeWeapon = Owner.EnsureComponent<MeleeWeaponComponent>();
            Owner.EntityManager.EventBus.SubscribeEvent<MeleeHitEvent>(EventSource.Local, this, OnMeleeHit);
            _solutionContainer = Owner.EnsureComponent<SolutionContainerComponent>();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Owner.EntityManager.EventBus.UnsubscribeEvent<MeleeHitEvent>(EventSource.Local, this);
        }

        private void OnMeleeHit(MeleeHitEvent hitEvent)
        {
            var hitEntities = hitEvent.HitEntities;
            var hitBloodstreams = new List<BloodstreamComponent>();
            foreach (var entity in hitEntities)
            {
                if (entity.TryGetComponent<BloodstreamComponent>(out var bloodstream))
                    hitBloodstreams.Add(bloodstream);
            }

            var removedSolution = _solutionContainer.Solution.SplitSolution(TransferAmount * hitBloodstreams.Count);
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
