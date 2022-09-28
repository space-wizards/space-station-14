using Content.Client.Box.Components;
using Content.Client.Interactable;
using Content.Shared.Box;
using Content.Shared.Box.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Box;

public sealed class BoxSystem : SharedBoxSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PlayBoxEffectMessage>(OnBoxEffect);
    }

    private void OnBoxEffect(PlayBoxEffectMessage msg)
    {
        if (!TryComp<BoxComponent>(msg.Source, out var box))
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(msg.Source, out var xform))
            return;

        //Any mob that can move should be surprised?
        //God mind rework needs to come faster so it can just check for mind
        var mobMoverEntities = new HashSet<EntityUid>();
        var mobMoverQuery = GetEntityQuery<MobMoverComponent>();

        foreach (var entity in _entityLookup.GetEntitiesInRange(xform.Coordinates, box.Distance))
        {
            if (!mobMoverQuery.HasComponent(entity))
                continue;

            mobMoverEntities.Add(entity);
        }

        foreach (var entity in mobMoverEntities)
        {
            if (!xformQuery.TryGetComponent(entity, out var moverTransform))
                continue;

            if(!msg.Source.InRangeUnOccluded(moverTransform.MapPosition, box.Distance))
                continue;

            var ent = Spawn(box.Effect, moverTransform.MapPosition);

            if (!xformQuery.TryGetComponent(ent, out var entTransform))
                continue;

            if (!TryComp<SpriteComponent>(ent, out var sprite))
                continue;

            sprite.Offset = new Vector2(0, 1);
            entTransform.AttachParent(entity);
        }
    }
}
