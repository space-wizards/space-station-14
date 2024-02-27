using Content.Server.Thief.Components;
using Content.Shared.Foldable;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Server.Mind;
using Content.Server.Roles;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Thief.Systems;

/// <summary>
/// <see cref="ThiefFultonComponent"/>
/// 
/// </summary>
public sealed class ThiefFultonSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefFultonComponent, RoundEndMessageEvent>(OnRoundEnd);
        SubscribeLocalEvent<ThiefFultonComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnRoundEnd(Entity<ThiefFultonComponent> fulton, ref RoundEndMessageEvent args)
    {

    }

    private void OnActivate(Entity<ThiefFultonComponent> fulton, ref ActivateInWorldEvent args)
    {
        var mind = _mind.GetMind(args.User);
        if (!HasComp<ThiefRoleComponent>(mind))
        {
            _audio.PlayPvs(fulton.Comp.AccessDeniedSound, fulton);
            _popup.PopupEntity(Loc.GetString("thief-fulton-access-denied"), fulton);
            return;
        }

        if (fulton.Comp.LinkedOwner == null)
        {
            _audio.PlayPvs(fulton.Comp.LinkSound, fulton);
            _popup.PopupEntity(Loc.GetString("thief-fulton-set"), fulton);
            fulton.Comp.LinkedOwner = mind;
        }
        else
        {
            _audio.PlayPvs(fulton.Comp.AccessDeniedSound, fulton);
            _popup.PopupEntity(Loc.GetString("thief-fulton-already-set"), fulton);
            return;
        }
    }
}
