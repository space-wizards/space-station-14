using Content.Shared.Paper;
using Content.Shared.UserInterface;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._Impstation.Illiterate;

public sealed class IlliterateSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IlliterateComponent, UserOpenActivatableUIAttemptEvent>(OnActivateUIAttempt);
    }

    private void OnActivateUIAttempt(Entity<IlliterateComponent> ent, ref UserOpenActivatableUIAttemptEvent args)
    {
        if (HasComp<PaperComponent>(args.Target))
        {
            args.Cancel();
            _popupSystem.PopupClient(Loc.GetString(ent.Comp.FailMsg), ent.Owner, ent.Owner);
        }
    }
};

