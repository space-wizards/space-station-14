using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Foam;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SolutionAreaEffectComponent))]
    public sealed class FoamSolutionAreaEffectComponent : SolutionAreaEffectComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public new const string SolutionName = "solutionArea";

        [DataField("foamedMetalPrototype")] private string? _foamedMetalPrototype;

        protected override void UpdateVisuals()
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance) &&
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            {
                appearance.SetData(FoamVisuals.Color, solution.GetColor(_proto).WithAlpha(0.80f));
            }
        }

        protected override void ReactWithEntity(EntityUid entity, double solutionFraction)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                return;

            if (!_entMan.TryGetComponent(entity, out BloodstreamComponent? bloodstream))
                return;

            var invSystem = EntitySystem.Get<InventorySystem>();

            // TODO: Add a permeability property to clothing
            // For now it just adds to protection for each clothing equipped
            var protection = 0f;
            if (invSystem.TryGetSlots(entity, out var slotDefinitions))
            {
                foreach (var slot in slotDefinitions)
                {
                    if (slot.Name == "back" ||
                        slot.Name == "pocket1" ||
                        slot.Name == "pocket2" ||
                        slot.Name == "id")
                        continue;

                    if (invSystem.TryGetSlotEntity(entity, slot.Name, out _))
                        protection += 0.025f;
                }
            }

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();

            var cloneSolution = solution.Clone();
            var transferAmount = FixedPoint2.Min(cloneSolution.Volume * solutionFraction * (1 - protection),
                bloodstream.ChemicalSolution.AvailableVolume);
            var transferSolution = cloneSolution.SplitSolution(transferAmount);

            if (bloodstreamSys.TryAddToChemicals(entity, transferSolution, bloodstream))
            {
                // Log solution addition by foam
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{_entMan.ToPrettyString(entity):target} was affected by foam {SolutionContainerSystem.ToPrettyString(transferSolution)}");
            }
        }

        protected override void OnKill()
        {
            if (_entMan.Deleted(Owner))
                return;
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(FoamVisuals.State, true);
            }

            Owner.SpawnTimer(600, () =>
            {
                if (!string.IsNullOrEmpty(_foamedMetalPrototype))
                {
                    _entMan.SpawnEntity(_foamedMetalPrototype, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                }

                _entMan.QueueDeleteEntity(Owner);
            });
        }
    }
}
