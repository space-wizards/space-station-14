using Content.Server.Whistle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;

namespace Content.Server.Whistle;

public sealed class WhistleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhistleComponent, UseInHandEvent>(OnUseInHand);
    }
    private bool ExclamateTarget(EntityUid target, WhistleComponent component)
    {
        SpawnAttachedTo(component.effect, target.ToCoordinates());

        return true;
    }
    public void OnUseInHand(EntityUid uid, WhistleComponent component, UseInHandEvent args)
    {
        if (component.Distance > 0)
            MakeLoudWhistle(uid, args.User, component);

        args.Handled = true;
    }
    private bool MakeLoudWhistle(EntityUid uid, EntityUid owner, WhistleComponent component)
    {
        foreach (var moverComponent in
            _entityLookup.GetComponentsInRange<MobMoverComponent>(Transform(uid).Coordinates, component.Distance))
        {
            if (moverComponent.Owner == owner)
                continue;

            ExclamateTarget(moverComponent.Owner, component);
        }

        return true;
    }
}
