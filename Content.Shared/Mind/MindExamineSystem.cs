using Content.Shared.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Mind;

public sealed class MindExamineSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindExaminableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MindExaminableComponent, ComponentStartup>((e, ref _) => RefreshMindStatus(e.AsNullable()));
        SubscribeLocalEvent<MindExaminableComponent, MindAddedMessage>((e, ref _) => RefreshMindStatus(e.AsNullable()));
        SubscribeLocalEvent<MindExaminableComponent, MindRemovedMessage>((e, ref _) => RefreshMindStatus(e.AsNullable()));
        SubscribeLocalEvent<MindExaminableComponent, MobStateChangedEvent>((e, ref _) => RefreshMindStatus(e.AsNullable()));

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnExamined(Entity<MindExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        switch (ent.Comp.State)
        {
            case MindState.Irrecoverable:
                args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-dead-and-irrecoverable", ("ent", ent.Owner))}[/color]");
                break;
            case MindState.DeadSSD:
                args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-dead-and-ssd", ("ent", ent.Owner))}[/color]");
                break;
            case MindState.Dead:
                args.PushMarkup($"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", ent.Owner))}[/color]");
                break;
            case MindState.Catatonic:
                args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", ent.Owner))}[/color]");
                break;
            case MindState.SSD:
                args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", ent.Owner))}[/color]");
                break;
        }
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (!_mind.TryGetMind(args.Entity, out _, out var mindComp))
            return;

        if (mindComp.OwnedEntity is not { } refreshEnt)
            return;

        RefreshMindStatus(refreshEnt);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        if (!_mind.TryGetMind(args.Entity, out _, out var mindComp))
            return;

        if (mindComp.OwnedEntity is not { } refreshEnt)
            return;

        RefreshMindStatus(refreshEnt);
    }


    public void RefreshMindStatus(Entity<MindExaminableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        // We don't let the client handle this.
        // This is because the mind is not networked, and the client will be always wrong.
        // So instead, we do this on server and dirty the result to the client.
        // And since it is stored on the component, the text won't flicker anymore.
        // Will cause slight mispredicts right after a state is changed due to networking, but I don't know a better way to handle this.
        if (_net.IsClient)
            return;

        var dead = _mobState.IsDead(ent);
        _mind.TryGetMind(ent.Owner, out _, out var mindComp);
        var hasUserId = mindComp?.UserId;
        var hasActiveSession = hasUserId != null && _playerManager.ValidSessionId(hasUserId.Value);

        // Scenarios:
        // 1. Dead + No User ID: Entity is permanently dead with no player ever attached
        // 2. Dead + Has User ID + No Session: Player died and disconnected
        // 3. Dead + Has Session: Player is dead but still connected
        // 4. Alive + No User ID: Entity was never controlled by a player
        // 5. Alive + No Session: Player disconnected while alive (SSD)

        if (dead && hasUserId == null)
            ent.Comp.State = MindState.Irrecoverable;
        else if (dead && !hasActiveSession)
            ent.Comp.State = MindState.DeadSSD;
        else if (dead)
            ent.Comp.State = MindState.Dead;
        else if (hasUserId == null)
            ent.Comp.State = MindState.Catatonic;
        else if (!hasActiveSession)
            ent.Comp.State = MindState.SSD;
        else
            ent.Comp.State = MindState.None;

        Dirty(ent);
    }
}
