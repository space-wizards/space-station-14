using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Stealth.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Whistle;

public sealed class WhistleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhistleComponent, UseInHandEvent>(OnUseInHand);
    }

    private void ExclamateTarget(EntityUid target, WhistleComponent component)
    {
        SpawnAttachedTo(component.Effect, target.ToCoordinates());
    }

    public void OnUseInHand(EntityUid uid, WhistleComponent component, UseInHandEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = TryMakeLoudWhistle(uid, args.User, component);
    }

    public bool TryMakeLoudWhistle(EntityUid uid, EntityUid owner, WhistleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Distance <= 0)
            return false;

        MakeLoudWhistle(uid, owner, component);
        return true;
    }

    private void MakeLoudWhistle(EntityUid uid, EntityUid owner, WhistleComponent component)
    {
        StealthComponent? stealth = null;

        foreach (var iterator in
            _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(_transform.GetMapCoordinates(uid), component.Distance))
        {
            //Avoid pinging invisible entities
            if (TryComp(iterator, out stealth) && stealth.Enabled)
                continue;

            //We don't want to ping user of whistle
            if (iterator.Owner == owner)
                continue;

            ExclamateTarget(iterator, component);
        }
    }
}
