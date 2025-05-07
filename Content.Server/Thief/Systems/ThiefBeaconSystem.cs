using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.Thief.Components;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Roles;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Thief.Systems;

/// <summary>
/// <see cref="ThiefBeaconComponent"/>
/// </summary>
public sealed class ThiefBeaconSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefBeaconComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<ThiefBeaconComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<ThiefBeaconComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetInteractionVerbs(Entity<ThiefBeaconComponent> beacon, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        if (TryComp<FoldableComponent>(beacon, out var foldable) && foldable.IsFolded)
            return;

        var mind = _mind.GetMind(args.User);
        if (mind == null || !_roles.MindHasRole<ThiefRoleComponent>(mind.Value))
            return;

        var user = args.User;
        args.Verbs.Add(new()
        {
            Act = () =>
            {
                SetCoordinate(beacon, mind.Value);
            },
            Message = Loc.GetString("thief-fulton-verb-message"),
            Text = Loc.GetString("thief-fulton-verb-text"),
        });
    }

    private void OnFolded(Entity<ThiefBeaconComponent> beacon, ref FoldedEvent args)
    {
        if (args.IsFolded)
            ClearCoordinate(beacon);
    }

    private void OnExamined(Entity<ThiefBeaconComponent> beacon, ref ExaminedEvent args)
    {
        if (!TryComp<StealAreaComponent>(beacon, out var area))
            return;

        args.PushText(Loc.GetString(area.Owners.Count == 0
            ? "thief-fulton-examined-unset"
            : "thief-fulton-examined-set"));
    }

    private void SetCoordinate(Entity<ThiefBeaconComponent> beacon, EntityUid mind)
    {
        if (!TryComp<StealAreaComponent>(beacon, out var area))
            return;

        _audio.PlayPvs(beacon.Comp.LinkSound, beacon);
        _popup.PopupEntity(Loc.GetString("thief-fulton-set"), beacon);
        area.Owners.Clear(); //We only reconfigure the beacon for ourselves, we don't need multiple thieves to steal from the same beacon.
        area.Owners.Add(mind);
    }

    private void ClearCoordinate(Entity<ThiefBeaconComponent> beacon)
    {
        if (!TryComp<StealAreaComponent>(beacon, out var area))
            return;

        if (area.Owners.Count == 0)
            return;

        _audio.PlayPvs(beacon.Comp.UnlinkSound, beacon);
        _popup.PopupEntity(Loc.GetString("thief-fulton-clear"), beacon);
        area.Owners.Clear();
    }
}
