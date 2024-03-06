using Content.Server.Thief.Components;
using Content.Shared.Foldable;
using Content.Shared.Popups;
using Content.Server.Mind;
using Content.Server.Roles;
using Robust.Shared.Audio.Systems;
using Content.Shared.Verbs;
using Content.Shared.Examine;

namespace Content.Server.Thief.Systems;

/// <summary>
/// <see cref="ThiefFultonComponent"/>
/// </summary>
public sealed class ThiefFultonSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefFultonComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<ThiefFultonComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<ThiefFultonComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ThiefFultonComponent> fulton, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString(fulton.Comp.LinkedOwner == null
                ? "thief-fulton-examined-unset"
                : "thief-fulton-examined-set"));
    }

    private void OnFolded(Entity<ThiefFultonComponent> fulton, ref FoldedEvent args)
    {
        if (args.IsFolded)
            ClearCoordinate(fulton);
    }

    private void OnGetInteractionVerbs(Entity<ThiefFultonComponent> fulton, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        if (TryComp<FoldableComponent>(fulton, out var foldable) && foldable.IsFolded)
            return;

        var mind = _mind.GetMind(args.User);
        if (!HasComp<ThiefRoleComponent>(mind))
            return;

        var user = args.User;
        args.Verbs.Add(new()
        {
            Act = () =>
            {
                SetCoordinate(fulton, mind.Value);
            },
            Message = Loc.GetString("thief-fulton-verb-message"),
            Text = Loc.GetString("thief-fulton-verb-text"),
        });
    }

    private void SetCoordinate(Entity<ThiefFultonComponent> fulton, EntityUid mind)
    {
        _audio.PlayPvs(fulton.Comp.LinkSound, fulton);
        _popup.PopupEntity(Loc.GetString("thief-fulton-set"), fulton);
        fulton.Comp.LinkedOwner = mind;
    }

    private void ClearCoordinate(Entity<ThiefFultonComponent> fulton)
    {
        if (fulton.Comp.LinkedOwner == null)
            return;

        _audio.PlayPvs(fulton.Comp.UnlinkSound, fulton);
        _popup.PopupEntity(Loc.GetString("thief-fulton-clear"), fulton);
        fulton.Comp.LinkedOwner = null;
    }
}
