using System;
using System.Linq;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class FoamComponent : SharedFoamComponent
    {
        private string _foamedMetalPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _foamedMetalPrototype, "foamedMetalPrototype", "");
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AreaEffectSpreadMessage spreadMessage:
                    HandleSpreadMessage(spreadMessage);
                    break;
                case AreaEffectReactMessage reactMessage:
                    HandleReactMessage(reactMessage);
                    break;
                case AreaEffectKillMessage:
                    HandleKillMessage();
                    break;
            }
        }

        private void HandleSpreadMessage(AreaEffectSpreadMessage spreadMessage)
        {
            foreach (var foamEntity in spreadMessage.Spawned)
            {
                if (foamEntity.TryGetComponent(out FoamComponent foamComp) &&
                    Owner.TryGetComponent(out SolutionContainerComponent contents))
                {
                    var solution = contents.Solution.Clone();
                    foamComp.TryAddSolution(solution);
                }
            }
        }

        private void HandleReactMessage(AreaEffectReactMessage reactMessage)
        {
            var averageExpositions = reactMessage.AverageExpositions;
            var mapManager = reactMessage.MapManager;
            var prototypeManager = reactMessage.PrototypeManager;

            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var mapGrid = mapManager.GetGrid(Owner.Transform.GridID);
            var tile = mapGrid.GetTileRef(Owner.Transform.Coordinates.ToVector2i(Owner.EntityManager, mapManager));

            var solutionFraction = 1 / Math.Floor(averageExpositions);

            foreach (var reagentQuantity in contents.ReagentList.ToArray())
            {
                if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                var reagent = prototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);

                // React with the tile the foam is on
                reagent.ReactionTile(tile, reagentQuantity.Quantity * solutionFraction);

                // Touch every entity on the tile
                foreach (var entity in tile.GetEntitiesInTileFast())
                {
                    reagent.ReactionEntity(entity, ReactionMethod.Touch, reagentQuantity.Quantity * solutionFraction);
                }
            }

            // Enter the bloodstream of every entity on the tile
            foreach (var entity in tile.GetEntitiesInTileFast())
            {
                if (!entity.TryGetComponent(out BloodstreamComponent bloodstream))
                    continue;

                // TODO: Add a permeability property to clothing
                // For now it just adds to protection for each clothing equipped
                var protection = 0f;
                if (entity.TryGetComponent(out InventoryComponent inventory))
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

                var cloneSolution = contents.Solution.Clone();
                var transferAmount = ReagentUnit.Min(cloneSolution.TotalVolume * solutionFraction * (1 - protection), bloodstream.EmptyVolume);
                var transferSolution = cloneSolution.SplitSolution(transferAmount);

                bloodstream.TryTransferSolution(transferSolution);
            }
        }

        private void HandleKillMessage()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(FoamVisuals.State, true);
            }
            Owner.SpawnTimer(600, () =>
            {
                if (!String.IsNullOrEmpty(_foamedMetalPrototype))
                {
                    Owner.EntityManager.SpawnEntity(_foamedMetalPrototype, Owner.Transform.Coordinates);
                }
                Owner.Delete();
            });
        }

        public void TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
                return;

            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var addSolution = solution.SplitSolution(ReagentUnit.Min(solution.TotalVolume,contents.EmptyVolume));

            var result = contents.TryAddSolution(addSolution);

            if (!result)
                return;

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(FoamVisuals.Color, contents.Color.WithAlpha(0.80f));
            }
        }
    }
}
