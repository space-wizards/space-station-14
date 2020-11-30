using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeChemicalInjectorComponent : Component
    {
        public override string Name => "MeleeChemicalInjector";

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; }

        public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }
        private float _transferEfficiency;

        [ViewVariables]
        private SolutionContainerComponent _solutionContainer;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => TransferAmount, "transferAmount", ReagentUnit.New(1));
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
            foreach (var entity in hitEntities)
            {
                if (!entity.TryGetComponent<BloodstreamComponent>(out var bloodstream))
                    return;
                var removedSolution = _solutionContainer.Solution.SplitSolution(TransferAmount);
                var removedVolume = removedSolution.TotalVolume;
                var solutionToInject = removedSolution.SplitSolution(removedVolume * TransferEfficiency);
                bloodstream.TryTransferSolution(solutionToInject);
            }
        }
    }
}
