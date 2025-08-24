using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem
{
    protected virtual void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        var explodeName = Loc.GetString("admin-smite-explode-name").ToLowerInvariant();
        Verb explode = new()
        {
            Text = explodeName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Act = () => SmiteExplodeVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", explodeName, Loc.GetString("admin-smite-explode-description")) // we do this so the description tells admins the Text to run it via console.
        };
        args.Verbs.Add(explode);

        var chessName = Loc.GetString("admin-smite-chess-dimension-name").ToLowerInvariant();
        Verb chess = new()
        {
            Text = chessName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Fun/Tabletop/chessboard.rsi"), "chessboard"),
            Act = () => SmiteChessVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", chessName, Loc.GetString("admin-smite-chess-dimension-description"))
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            var flamesName = Loc.GetString("admin-smite-set-alight-name").ToLowerInvariant();
            Verb flames = new()
            {
                Text = flamesName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Alerts/Fire/fire.png")),
                Act = () =>
                {
                    // Fuck you. Burn Forever.
                    flammable.FireStacks = flammable.MaximumFireStacks;
                    _flammableSystem.Ignite(args.Target, args.User);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-set-alight-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-set-alight-others", ("name", args.Target)), xform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", flamesName, Loc.GetString("admin-smite-set-alight-description"))
            };
            args.Verbs.Add(flames);
        }
    }

    protected virtual void SmiteExplodeVerb(EntityUid target)
    {
    }

    protected virtual void SmiteChessVerb(EntityUid target)
    {
    }
}
