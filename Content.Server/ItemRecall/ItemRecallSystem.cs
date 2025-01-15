using Content.Shared.Hands.EntitySystems;
using Content.Shared.ItemRecall;
using Content.Shared.Popups;
using Content.Shared.Projectiles;

namespace Content.Server.ItemRecall;

/// <summary>
/// System for handling the ItemRecall ability for wizards.
/// </summary>
public sealed partial class ItemRecallSystem : SharedItemRecallSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedProjectileSystem _proj = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecallMarkerComponent, RecallItemEvent>(OnItemRecall);
    }

    private void OnItemRecall(Entity<RecallMarkerComponent> ent, ref RecallItemEvent args)
    {
        RecallItem(ent);
    }

    private void RecallItem(Entity<RecallMarkerComponent> ent)
    {
        if (ent.Comp.MarkedByEntity == null)
            return;

        if (TryComp<EmbeddableProjectileComponent>(ent, out var projectile))
            _proj.UnEmbed(ent, projectile, ent.Comp.MarkedByEntity.Value);

        _popups.PopupEntity(Loc.GetString("item-recall-item-summon", ("item", ent)), ent.Comp.MarkedByEntity.Value, ent.Comp.MarkedByEntity.Value);

        _hands.TryForcePickupAnyHand(ent.Comp.MarkedByEntity.Value, ent);
    }
}
