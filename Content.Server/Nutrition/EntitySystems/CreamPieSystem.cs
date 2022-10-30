using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class CreamPieSystem : SharedCreamPieSystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CreamPiedComponent, RejuvenateEvent>(OnRejuvenate);
        }

        protected override void SplattedCreamPie(EntityUid uid, CreamPieComponent creamPie)
        {
            _audioSystem.Play(creamPie.Sound, Filter.Pvs(creamPie.Owner), creamPie.Owner, AudioParams.Default.WithVariation(0.125f));

            if (EntityManager.TryGetComponent<FoodComponent?>(creamPie.Owner, out var foodComp) && _solutionsSystem.TryGetSolution(creamPie.Owner, foodComp.SolutionName, out var solution))
            {
                _spillableSystem.SpillAt(creamPie.Owner, solution, "PuddleSmear", false);
            }

            EntityManager.QueueDeleteEntity(uid);
        }

        protected override void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("cream-pied-component-on-hit-by-message", ("thrower", args.Thrown)),
                creamPied.Owner,
                Filter.Entities(creamPied.Owner)
            );
            _popupSystem.PopupEntity(
                Loc.GetString("cream-pied-component-on-hit-by-message-others", ("owner", creamPied.Owner),("thrower", args.Thrown)),
                creamPied.Owner,
                Filter.PvsExcept(creamPied.Owner)
            );
        }

        private void OnRejuvenate(EntityUid uid, CreamPiedComponent component, RejuvenateEvent args)
        {
            SetCreamPied(uid, component, false);
        }
    }
}
