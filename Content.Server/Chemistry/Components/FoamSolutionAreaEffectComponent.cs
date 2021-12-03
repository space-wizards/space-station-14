using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Shared.FixedPoint;
using Content.Shared.Foam;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public class FoamSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        public override string Name => "FoamSolutionAreaEffect";
        public new const string SolutionName = "solutionArea";

        [DataField("foamedMetalPrototype")] private string? _foamedMetalPrototype;

        protected override void UpdateVisuals()
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Uid, out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
            {
                appearance.SetData(FoamVisuals.Color, solution.Color.WithAlpha(0.80f));
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
                return;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity.Uid, out BloodstreamComponent? bloodstream))
                return;

            // TODO: Add a permeability property to clothing
            // For now it just adds to protection for each clothing equipped
            var protection = 0f;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity.Uid, out InventoryComponent? inventory))
            {
                foreach (var slot in inventory.Slots)
                {
                    if (slot == EquipmentSlotDefines.Slots.BACKPACK ||
                        slot == EquipmentSlotDefines.Slots.POCKET1 ||
                        slot == EquipmentSlotDefines.Slots.POCKET2 ||
                        slot == EquipmentSlotDefines.Slots.IDCARD)
                        continue;

                    if (inventory.TryGetSlotItem(slot, out ItemComponent? _))
                        protection += 0.025f;
                }
            }

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();

            var cloneSolution = solution.Clone();
            var transferAmount = FixedPoint2.Min(cloneSolution.TotalVolume * solutionFraction * (1 - protection),
                bloodstream.Solution.AvailableVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            bloodstreamSys.TryAddToBloodstream(entity.Uid, transferSolution, bloodstream);
        }

        protected override void OnKill()
        {
            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                return;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(FoamVisuals.State, true);
            }

            Owner.SpawnTimer(600, () =>
            {
                if (!string.IsNullOrEmpty(_foamedMetalPrototype))
                {
                    IoCManager.Resolve<IEntityManager>().SpawnEntity(_foamedMetalPrototype, Owner.Transform.Coordinates);
                }

                IoCManager.Resolve<IEntityManager>().QueueDeleteEntity(Owner.Uid);
            });
        }
    }
}
