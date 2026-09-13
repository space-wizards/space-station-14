using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Burns a body: drops its inventory next to it, shows a popup and deletes it.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class BurnBodyEntityEffectSystem : EntityEffectSystem<TransformComponent, BurnBody>
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<BurnBody> args)
    {
        var bodyId = entity.Owner;

        if (HasComp<InventoryComponent>(bodyId))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
            {
                _transform.DropNextTo(item, bodyId);
            }
        }

        var bodyIdentity = Identity.Entity(bodyId, EntityManager);
        _popup.PopupCoordinates(
            Loc.GetString(args.Effect.PopupMessage, ("name", bodyIdentity)),
            _transform.GetMoverCoordinates(bodyId),
            PopupType.LargeCaution);

        PredictedQueueDel(bodyId);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class BurnBody : EntityEffectBase<BurnBody>
{
    /// <summary>
    /// The popup displayed upon applying this effect.
    /// </summary>
    [DataField]
    public LocId PopupMessage = "bodyburn-text-others";
}
