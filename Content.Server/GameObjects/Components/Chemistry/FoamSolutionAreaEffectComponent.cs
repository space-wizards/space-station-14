#nullable enable
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public class FoamSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        public override string Name => "FoamSolutionAreaEffect";

        private string? _foamedMetalPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _foamedMetalPrototype, "foamedMetalPrototype", null);
        }

        protected override void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance) &&
                SolutionContainerComponent != null)
            {
                appearance.SetData(FoamVisuals.Color, SolutionContainerComponent.Color.WithAlpha(0.80f));
            }
        }

        protected override void ReactWithEntity(IEntity entity, double solutionFraction)
        {
            if (SolutionContainerComponent == null)
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

            var cloneSolution = SolutionContainerComponent.Solution.Clone();
            var transferAmount = ReagentUnit.Min(cloneSolution.TotalVolume * solutionFraction * (1 - protection), bloodstream.EmptyVolume);
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
                Owner.Delete();
            });
        }
    }
}
