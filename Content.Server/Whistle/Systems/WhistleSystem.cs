using Content.Server.Whistle.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Whistle;

public sealed class WhistleSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhistleComponent, UseInHandEvent>(OnUseInHand);
    }
    private bool ExclamateTarget(EntityUid target, WhistleComponent component)
    {
        var effect = Spawn(component.effect, Transform(target).MapPosition);
        _transform.SetParent(effect, target);

        return true;
    }
    public void OnUseInHand(EntityUid uid, WhistleComponent component, UseInHandEvent args)
    {
        if (component.Distance > 0)
            MakeLoudWhistle(uid, args.User, component);

        _audio.PlayPvs(component.Sound, uid);

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
