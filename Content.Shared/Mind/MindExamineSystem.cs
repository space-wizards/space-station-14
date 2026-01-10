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
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

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

        var message = ent.Comp.State switch
        {
            MindState.Irrecoverable => $"[color=mediumpurple]{Loc.GetString("comp-mind-examined-dead-and-irrecoverable", ("ent", ent.Owner))}[/color]",
            MindState.DeadSSD => $"[color=yellow]{Loc.GetString("comp-mind-examined-dead-and-ssd", ("ent", ent.Owner))}[/color]",
            MindState.Dead => $"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", ent.Owner))}[/color]",
            MindState.Catatonic => $"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", ent.Owner))}[/color]",
            MindState.SSD => $"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", ent.Owner))}[/color]",
            _ => null,
        };

        if (message != null)
            args.PushMarkup(message);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        // We use the broadcasted event because we need to access the body of a ghost if it disconnects.
        // DeadSSD does not check if a player is attached, but if the session is valid (connected).
        // To properly track that, we subscribe to the broadcast version of this event
        // and update the mind status of the original entity accordingly.
        // Otherwise, if you ghost out and THEN disconnect, it would not update your status as it gets raised on your ghost and not your body.
        if (!_mind.TryGetMind(args.Entity, out _, out var mindComp))
            return;

        if (mindComp.OwnedEntity is not { } refreshEnt)
            return;

        RefreshMindStatus(refreshEnt);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        // Same reason as in the subscription above.
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

        // Only allow the local client to handle this.
        // This is because the mind is only networked to the owner, and other clients will always be wrong.
        // So instead, we do this on server and dirty the result to the client.
        // And since it is stored on the component, the text won't flicker anymore.
        // Will cause a small jump when examined during networking due to the server update coming in.
        if (_net.IsClient && _player.LocalEntity != ent)
            return;

        var dead = _mobState.IsDead(ent);
        _mind.TryGetMind(ent.Owner, out _, out var mindComp);
        var hasUserId = mindComp?.UserId;
        var hasActiveSession = hasUserId != null && _player.ValidSessionId(hasUserId.Value);

        // Scenarios:
        // 1. Dead + No User ID: Entity is dead and has no mind attached
        // 2. Dead + Has User ID + No Session: Player died and disconnected
        // 3. Dead + Has Session: Player is dead but still connected
        // 4. Alive + No User ID: Entity is alive but has no mind attached to it
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
