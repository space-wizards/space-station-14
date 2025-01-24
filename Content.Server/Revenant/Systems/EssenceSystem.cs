using Content.Shared.Revenant.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Revenant.Systems;
using Robust.Shared.Random;

namespace Content.Server.Revenant.Systems;

/// <summary>
///     Attached to entities when a revenant drains them in order to manage their essence.
/// </summary>
public sealed class EssenceSystem : SharedEssenceSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EssenceComponent, ComponentStartup>(OnEssenceEventReceived);
        SubscribeLocalEvent<EssenceComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<EssenceComponent, MindAddedMessage>(OnEssenceEventReceived);
        SubscribeLocalEvent<EssenceComponent, MindRemovedMessage>(OnEssenceEventReceived);
    }

    private void OnEssenceEventReceived<T>(Entity<EssenceComponent> ent, ref T ev)
    {
        UpdateEssenceAmount(ent);
    }

    private void OnMobStateChanged(Entity<EssenceComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateEssenceAmount(ent);
    }

    private void UpdateEssenceAmount(Entity<EssenceComponent> ent)
    {
        if (!TryComp<MobStateComponent>(ent, out var mob))
            return;

        var hasMind = TryComp<MindContainerComponent>(ent, out var mind) && mind.Mind != null;
        var ranges = hasMind
            ? ent.Comp.MindfulEssenceRanges
            : ent.Comp.MindlessEssenceRanges;

        if (ranges.TryGetValue(mob.CurrentState, out var range))
            ent.Comp.EssenceAmount = _random.NextFloat(range.X, range.Y);
    }
}
