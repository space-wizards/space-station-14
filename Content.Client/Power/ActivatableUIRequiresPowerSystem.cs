using Content.Client.Power.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Wires;

namespace Content.Client.Power;

public sealed class ActivatableUIRequiresPowerSystem : SharedActivatableUIRequiresPowerSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnActivate(Entity<ActivatableUIRequiresPowerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || this.IsPowered(ent.Owner, EntityManager))
        {
            return;
        }

        _popup.PopupClient(Loc.GetString("base-computer-ui-component-not-powered", ("machine", ent.Owner)), args.User, args.User);
        args.Cancel();
    }

    protected override void OnActivateVerb(Entity<ActivatableUIRequiresPowerComponent> ent, ref VerbUIOpenAttemptEvent args)
    {
        if (args.Cancelled || this.IsPowered(ent.Owner, EntityManager))
            return;

        args.Cancel();
        args.CancelReason = Loc.GetString("base-computer-ui-component-not-powered");
    }
}
