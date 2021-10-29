using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class CreamPieSystem : SharedCreamPieSystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;

        protected override void SplattedCreamPie(EntityUid uid, CreamPieComponent creamPie)
        {
            SoundSystem.Play(Filter.Pvs(creamPie.Owner), creamPie.Sound.GetSound(), creamPie.Owner, AudioHelpers.WithVariation(0.125f));

            if (creamPie.Owner.TryGetComponent<FoodComponent>(out var foodComp) && _solutionsSystem.TryGetSolution(creamPie.Owner, foodComp.SolutionName, out var solution))
            {
                solution.SpillAt(creamPie.Owner, "PuddleSmear", false);
            }
        }

        protected override void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            creamPied.Owner.PopupMessage(Loc.GetString("cream-pied-component-on-hit-by-message",("thrower", args.Thrown)));
            creamPied.Owner.PopupMessageOtherClients(Loc.GetString("cream-pied-component-on-hit-by-message-others", ("owner", creamPied.Owner),("thrower", args.Thrown)));
        }
    }
}
