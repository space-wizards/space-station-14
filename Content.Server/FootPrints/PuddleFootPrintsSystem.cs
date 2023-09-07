using Content.Shared.FootPrints;
using Content.Shared.Fluids;
using Robust.Shared.Physics.Events;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.FootPrints
{
    public sealed class PuddleFootPrintsSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PuddleFootPrintsComponent, EndCollideEvent>(OnStepTrigger);
        }

        public void OnStepTrigger(EntityUid uid, PuddleFootPrintsComponent comp, ref EndCollideEvent args)
        {
            if (!_configManager.GetCVar(CCVars.FootPrintsEnabled))
                return;
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            if (!TryComp<FootPrintsComponent>(args.OtherEntity, out var tripper))
                return;
            if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager))
                return;
            if (_solutionContainerSystem.TryGetSolution(uid, "puddle", out var solutions, solutionManager))
            {
                var listSolutions = solutions.Contents.ToArray();
                var fullSolutionsQuantity = 0f;
                var waterQuantity = 0f;
                foreach (var sol in listSolutions)
                {
                    fullSolutionsQuantity += (float) sol.Quantity;
                    if (sol.Reagent.Prototype == "Water")
                        waterQuantity = (float) sol.Quantity;
                }
                if (waterQuantity / (fullSolutionsQuantity / 100f) > comp.OffPercent)
                    return;

            }
            if
                (
                    _appearance.TryGetData(uid, PuddleVisuals.SolutionColor, out var color, appearance) &&
                    _appearance.TryGetData(uid, PuddleVisuals.CurrentVolume, out var volume, appearance)
                )
            {
                //tripper.PrintsColor = (Color)color;
                AddColor((Color) color, (float) volume * comp.SizeRatio, tripper);
                //PuddleVisuals.CurrentVolume
            }

        }

        private void AddColor(Color col, float quantity, FootPrintsComponent comp)
        {
            if (comp.ColorQuantity == 0f)
            {
                comp.PrintsColor = col;
            }
            else
            {
                comp.PrintsColor = Color.InterpolateBetween(comp.PrintsColor, col, 0.2f);
            }
            comp.ColorQuantity += quantity;
        }
    }
}
