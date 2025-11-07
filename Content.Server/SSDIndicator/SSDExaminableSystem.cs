using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.SSDIndicator;
using Robust.Shared.Player;

namespace Content.Server.SSDIndicator;

public sealed class SSDExaminableSystem : EntitySystem
{

    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDExaminableComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SSDExaminableComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SSDExaminableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SSDExaminableComponent, MindAddedMessage>(OnAdded);
        SubscribeLocalEvent<SSDExaminableComponent, MindRemovedMessage>(OnRemoved);
    }


    private void OnAdded(Entity<SSDExaminableComponent> ent, ref MindAddedMessage args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var _, out var mind))
            ent.Comp.Status = SSDStatus.Catatonic;
        else if (mind.UserId is not null && !_playerManager.ValidSessionId(mind.UserId.Value))
            ent.Comp.Status = SSDStatus.SSD;
        else
            ent.Comp.Status = SSDStatus.Normal;

        Dirty(ent);
    }

    private void OnRemoved(Entity<SSDExaminableComponent> ent, ref MindRemovedMessage args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var _, out var mind))
            ent.Comp.Status = SSDStatus.Catatonic;
        else if (mind.UserId is not null && !_playerManager.ValidSessionId(mind.UserId.Value))
            ent.Comp.Status = SSDStatus.SSD;
        else
            ent.Comp.Status = SSDStatus.Normal;

        Dirty(ent);
    }

    private void OnPlayerAttached(Entity<SSDExaminableComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var _, out var mind))
            ent.Comp.Status = SSDStatus.Catatonic;
        else if (mind.UserId is not null && !_playerManager.ValidSessionId(mind.UserId.Value))
            ent.Comp.Status = SSDStatus.SSD;
        else
            ent.Comp.Status = SSDStatus.Normal;

        Dirty(ent);
    }

    private void OnPlayerDetached(Entity<SSDExaminableComponent> ent, ref PlayerDetachedEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var _, out var mind))
            ent.Comp.Status = SSDStatus.Catatonic;
        else if (mind.UserId is not null && !_playerManager.ValidSessionId(mind.UserId.Value))
            ent.Comp.Status = SSDStatus.SSD;
        else
            ent.Comp.Status = SSDStatus.Normal;

        Dirty(ent);
    }

    private void OnMapInit(Entity<SSDExaminableComponent> ent, ref MapInitEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var _, out var mind))
            ent.Comp.Status = SSDStatus.Catatonic;
        else if (mind.UserId is not null && !_playerManager.ValidSessionId(mind.UserId.Value))
            ent.Comp.Status = SSDStatus.SSD;
        else
            ent.Comp.Status = SSDStatus.Normal;

        Dirty(ent);
    }
}
