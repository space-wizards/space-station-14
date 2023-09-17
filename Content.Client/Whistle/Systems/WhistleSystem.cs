using Content.Shared.Whistle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Components;
using Content.Shared.Whistle.Events;

namespace Content.Client.Whistle;

public sealed class WhistleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<OnWhistleEvent>(OnWhistle);
    }
    private bool ExclamateTarget(EntityUid target, WhistleComponent component)
    {
        SpawnAttachedTo(component.effect, target.ToCoordinates());
        return true;
    }
    public void OnWhistle(OnWhistleEvent args)
    {
        TryMakeLoudWhistle(args.Source, args.User, EntityManager.GetComponent<WhistleComponent>(args.Source));
    }
    public bool TryMakeLoudWhistle(EntityUid uid, EntityUid user, WhistleComponent component)
    {
        if (component.Distance <= 0)
            return false;

        MakeLoudWhistle(uid, user, component);
        return true;
    }
    private bool MakeLoudWhistle(EntityUid uid, EntityUid user, WhistleComponent component)
    {
        foreach (var moverComponent in
            _entityLookup.GetComponentsInRange<MobMoverComponent>(Transform(uid).Coordinates, component.Distance))
        {
            
            if (moverComponent.Owner == user)
                continue;

            ExclamateTarget(moverComponent.Owner, component);
        }

        return true;
    }
}
