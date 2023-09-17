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
    private bool ExclamateTarget(EntityUid uid, EntityUid target, WhistleComponent component)
    {
        TransformComponent? targetTransform = null;

        if (!Resolve(target, ref targetTransform))
            return false;

        var effect = Spawn(component.effect, targetTransform.MapPosition);
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
        var mobMoverEntities = new HashSet<EntityUid>();
        TransformComponent? whistleTransform = null;

        if (!Resolve(uid, ref whistleTransform))
            return false;

        foreach (var moverComponent in
            _entityLookup.GetComponentsInRange<MobMoverComponent>(whistleTransform.Coordinates, component.Distance))
        {
            mobMoverEntities.Add(moverComponent.Owner);
        }

        mobMoverEntities.Remove(owner);

        foreach (var entity in mobMoverEntities)
            ExclamateTarget(uid, entity, component);

        return true;
    }
}
