using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.MusicPlayer;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Verbs;
using Robust.Shared.Localization;
using Content.Server.Popups;

namespace Content.Server.MusicPlayer;

public sealed class MusicRadioSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MusicRadioComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MusicRadioComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<MusicRadioComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerb);
    }

    private void OnUseInHand(EntityUid uid, MusicRadioComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_players.TryGetSessionByEntity(args.User, out var session))
        {
            RaiseNetworkEvent(new OpenMusicPlayerEvent(), session);
            _popup.PopupEntity(Loc.GetString("Opening music player"), uid, args.User);
            args.Handled = true;
        }
    }

    private void OnActivate(EntityUid uid, MusicRadioComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (_players.TryGetSessionByEntity(args.User, out var session))
        {
            RaiseNetworkEvent(new OpenMusicPlayerEvent(), session);
            args.Handled = true;
        }
    }

    private void OnGetInteractionVerb(EntityUid uid, MusicRadioComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new InteractionVerb
        {
            Text = Loc.GetString("Open Music Player"),
            Act = () =>
            {
                if (_players.TryGetSessionByEntity(args.User, out var session))
                {
                    RaiseNetworkEvent(new OpenMusicPlayerEvent(), session);
                    _popup.PopupEntity(Loc.GetString("Opening music player"), uid, args.User);
                }
            }
        };

        args.Verbs.Add(verb);
    }
}
