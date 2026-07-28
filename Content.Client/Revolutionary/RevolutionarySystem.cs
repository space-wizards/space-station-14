using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : SharedRevolutionarySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    // DS14-start
    private readonly Dictionary<NetEntity, ProtoId<FactionIconPrototype>> _revolutionaries = new();
    private readonly Dictionary<NetEntity, ProtoId<FactionIconPrototype>> _headRevolutionaries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RevolutionaryRosterSyncEvent>(OnRosterSync);
        SubscribeNetworkEvent<RevolutionaryRosterDeltaEvent>(OnRosterDelta);
        SubscribeNetworkEvent<RevolutionaryRosterClearEvent>(_ => ClearRoster());
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(_ => ClearRoster());
        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(GetRevolutionaryIcon);
        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(GetReplayRevolutionaryIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>(GetReplayHeadRevolutionaryIcon);
    }

    private void OnRosterSync(RevolutionaryRosterSyncEvent ev)
    {
        _revolutionaries.Clear();
        foreach (var (entity, icon) in ev.Revolutionaries)
        {
            _revolutionaries[entity] = icon;
        }

        _headRevolutionaries.Clear();
        foreach (var (entity, icon) in ev.HeadRevolutionaries)
        {
            _headRevolutionaries[entity] = icon;
        }
    }

    private void OnRosterDelta(RevolutionaryRosterDeltaEvent ev)
    {
        foreach (var (entity, icon) in ev.AddedRevolutionaries)
        {
            _revolutionaries[entity] = icon;
        }

        foreach (var entity in ev.RemovedRevolutionaries)
        {
            _revolutionaries.Remove(entity);
        }

        foreach (var (entity, icon) in ev.AddedHeadRevolutionaries)
        {
            _headRevolutionaries[entity] = icon;
        }

        foreach (var entity in ev.RemovedHeadRevolutionaries)
        {
            _headRevolutionaries.Remove(entity);
        }
    }

    private void ClearRoster()
    {
        _revolutionaries.Clear();
        _headRevolutionaries.Clear();
    }

    private void GetRevolutionaryIcon(Entity<StatusIconComponent> ent, ref GetStatusIconsEvent args)
    {
        var netEntity = GetNetEntity(ent.Owner);
        if (_headRevolutionaries.TryGetValue(netEntity, out var headIcon))
        {
            AddIcon(headIcon, ref args);
            return;
        }

        if (_revolutionaries.TryGetValue(netEntity, out var icon))
            AddIcon(icon, ref args);
    }

    private void GetReplayRevolutionaryIcon(Entity<RevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!HasComp<HeadRevolutionaryComponent>(ent))
            AddIcon(ent.Comp.StatusIcon, ref args);
    }

    private void GetReplayHeadRevolutionaryIcon(
        Entity<HeadRevolutionaryComponent> ent,
        ref GetStatusIconsEvent args)
    {
        AddIcon(ent.Comp.StatusIcon, ref args);
    }

    private void AddIcon(ProtoId<FactionIconPrototype> icon, ref GetStatusIconsEvent args)
    {
        if (_prototype.Resolve(icon, out var prototype))
            args.StatusIcons.Add(prototype);
    }
    // DS14-end
}
