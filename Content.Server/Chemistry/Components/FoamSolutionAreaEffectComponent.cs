using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Foam;
using Content.Shared.Inventory;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public class FoamSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        public override string Name => "FoamSolutionAreaEffect";
        public new const string SolutionName = "foam";

        [DataField("foamedMetalPrototype")] private string? _foamedMetalPrototype;

        protected override void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
            {
                appearance.SetData(FoamVisuals.Color, solution.Color.WithAlpha(0.80f));
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution))
                return;

            if (!entity.TryGetComponent(out BloodstreamComponent? bloodstream))
                return;

            // TODO: Add a permeability property to clothing
            // For now it just adds to protection for each clothing equipped
            var protection = 0f;
            if (entity.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var slot in inventory.Slots)
                {
                    if (slot == EquipmentSlotDefines.Slots.BACKPACK ||
                        slot == EquipmentSlotDefines.Slots.POCKET1 ||
                        slot == EquipmentSlotDefines.Slots.POCKET2 ||
                        slot == EquipmentSlotDefines.Slots.IDCARD)
                        continue;

                    if (inventory.TryGetSlotItem(slot, out ItemComponent _))
                        protection += 0.025f;
                }
            }

            var cloneSolution = solution.Clone();
            var transferAmount = FixedPoint2.Min(cloneSolution.TotalVolume * solutionFraction * (1 - protection),
                bloodstream.EmptyVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            bloodstream.TryTransferSolution(transferSolution);
        }

        protected override void OnKill()
        {
            if (Owner.Deleted)
                return;
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(FoamVisuals.State, true);
            }

            Owner.SpawnTimer(600, () =>
            {
                if (!string.IsNullOrEmpty(_foamedMetalPrototype))
                {
                    Owner.EntityManager.SpawnEntity(_foamedMetalPrototype, Owner.Transform.Coordinates);
                }

                Owner.QueueDelete();
            });
        }
    }
}
