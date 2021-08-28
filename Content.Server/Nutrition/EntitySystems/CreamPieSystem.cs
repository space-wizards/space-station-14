using Content.Server.Chemistry.Components;
using Content.Server.Fluids.Components;
using Content.Server.Notification;
using Content.Shared.Audio;
using Content.Shared.Notification.Managers;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class CreamPieSystem : SharedCreamPieSystem
    {
        protected override void SplattedCreamPie(EntityUid uid, CreamPieComponent creamPie)
        {
            SoundSystem.Play(Filter.Pvs(creamPie.Owner), creamPie.Sound.GetSound(), creamPie.Owner, AudioHelpers.WithVariation(0.125f));

            if (ComponentManager.TryGetComponent(uid, out SolutionContainerComponent? solution))
            {
                solution.Solution.SpillAt(creamPie.Owner, "PuddleSmear", false);
            }
        }

        protected override void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            creamPied.Owner.PopupMessage(Loc.GetString("cream-pied-component-on-hit-by-message",("thrower", args.Thrown)));
            creamPied.Owner.PopupMessageOtherClients(Loc.GetString("cream-pied-component-on-hit-by-message-others", ("owner", creamPied.Owner),("thrower", args.Thrown)));
        }
    }
}
