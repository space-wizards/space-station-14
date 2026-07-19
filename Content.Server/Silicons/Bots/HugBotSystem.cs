using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;
using Content.Shared.Silicons.Bots;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.Bots;

/// <summary>
/// Beyond what <see cref="SharedHugBotSystem"/> does, this system manages the "lifecycle" of
/// <see cref="RecentlyHuggedByHugBotComponent"/>.
/// </summary>
public sealed partial class HugBotSystem : SharedHugBotSystem
{
    private static readonly EntityTimerId HugCooldownTimer = new("hug-cooldown");

    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HugBotComponent, HTNRaisedEvent>(OnHtnRaisedEvent);
        SubscribeLocalEvent<RecentlyHuggedByHugBotComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnHtnRaisedEvent(Entity<HugBotComponent> entity, ref HTNRaisedEvent args)
    {
        if (args.Args is not HugBotDidHugEvent ||
            args.Target is not {} target)
            return;

        var ev = new HugBotHugEvent(GetNetEntity(entity));
        RaiseLocalEvent(target, ev);

        ApplyHugBotCooldown(entity, target);
    }

    /// <summary>
    /// Applies <see cref="RecentlyHuggedByHugBotComponent"/> to <paramref name="target"/> based on the configuration of
    /// <paramref name="hugBot"/>.
    /// </summary>
    public void ApplyHugBotCooldown(Entity<HugBotComponent> hugBot, EntityUid target)
    {
        var hugged = EnsureComp<RecentlyHuggedByHugBotComponent>(target);
        hugged.CooldownCompleteAfter = _timers.SetTimer<RecentlyHuggedByHugBotComponent>((target, hugged),
            HugCooldownTimer, hugBot.Comp.HugCooldown);
    }

    private void OnTimer(Entity<RecentlyHuggedByHugBotComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == HugCooldownTimer)
            RemCompDeferred<RecentlyHuggedByHugBotComponent>(ent);
    }
}

/// <summary>
/// This event is indirectly raised (by being <see cref="HTNRaisedEvent.Args"/>) on a HugBot when it hugs (or emaggedly
/// punches) an entity.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class HugBotDidHugEvent : EntityEventArgs;
