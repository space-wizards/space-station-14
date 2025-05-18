using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;
using Content.Shared.Silicons.Bots;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.Bots;

/// <summary>
/// Beyond what <see cref="SharedHugBotSystem"/> does, this system manages the "lifecycle" of
/// <see cref="RecentlyHuggedByHugBotComponent"/>.
/// </summary>
public sealed class HugBotSystem : SharedHugBotSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HugBotComponent, HTNRaisedEvent>(OnHtnRaisedEvent);
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
        hugged.CooldownCompleteAfter = _gameTiming.CurTime + hugBot.Comp.HugCooldown;
    }

    public override void Update(float frameTime)
    {
        // Iterate through all RecentlyHuggedByHugBot entities...
        var huggedEntities = AllEntityQuery<RecentlyHuggedByHugBotComponent>();
        while (huggedEntities.MoveNext(out var huggedEnt, out var huggedComp))
        {
            // ... and if their cooldown is complete...
            if (huggedComp.CooldownCompleteAfter <= _gameTiming.CurTime)
            {
                // ... remove it, allowing them to receive the blessing of hugs once more.
                RemCompDeferred<RecentlyHuggedByHugBotComponent>(huggedEnt);
            }
        }
    }
}

/// <summary>
/// This event is indirectly raised (by being <see cref="HTNRaisedEvent.Args"/>) on a HugBot when it hugs (or emaggedly
/// punches) an entity.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class HugBotDidHugEvent : EntityEventArgs;
