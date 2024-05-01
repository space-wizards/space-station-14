using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Robust.Shared.Timing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content.Server.ReagentOnItem;

public sealed class SpaceGlueOnItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceGlueOnItemComponent, GotEquippedHandEvent>(OnHandPickUp);
        SubscribeLocalEvent<SpaceGlueOnItemComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpaceGlueOnItemComponent, UnremoveableComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var glue, out var _, out var meta))
        {
            if (_timing.CurTime < glue.TimeOfNextCheck)
                continue;

            if (!SetNextNextCanDropCheck(glue, uid))
            {
                RemCompDeferred<UnremoveableComponent>(uid);
                RemCompDeferred<SpaceGlueOnItemComponent>(uid);
            }
        }
    }

    private void OnHandPickUp(Entity<SpaceGlueOnItemComponent> entity, ref GotEquippedHandEvent args)
    {
        _inventory.TryGetSlotEntity(args.User, "gloves", out var gloves);
        if (HasComp<NonStickSurfaceComponent>(gloves))
            return;

        if (SetNextNextCanDropCheck(entity, entity))
        {
            _popup.PopupEntity(Loc.GetString("space-glue-on-item-hand-stuck", ("target", Identity.Entity(entity, EntityManager))), args.User, args.User, PopupType.MediumCaution);
        }
    }

    /// <summary>
    ///     Sets the next time we will check in Update to see if the item is
    ///     still stuck or if there is no reagent left and it becomes droppable.
    /// </summary>
    /// <returns> Will return true if the item should still be stuck and false if the item should be droppable (There is less than 1 unit of reagent remaning). </returns>
    private bool SetNextNextCanDropCheck(SpaceGlueOnItemComponent glueComp, EntityUid uid)
    {

        if (glueComp.EffectStacks < 1)
            return false;

        var unremoveComp = EnsureComp<UnremoveableComponent>(uid);
        unremoveComp.DeleteOnDrop = false;

        glueComp.TimeOfNextCheck = _timing.CurTime + glueComp.DurationPerUnit;
        glueComp.EffectStacks = glueComp.EffectStacks - 1;

        return true;
    }

    private void OnExamine(EntityUid uid, SpaceGlueOnItemComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var howSticky = comp.EffectStacks / comp.MaxStacks;

        if (howSticky <= .33)
            args.PushMarkup(Loc.GetString("space-glue-on-item-inspect-low"));
        else if (howSticky <= .66)
            args.PushMarkup(Loc.GetString("space-glue-on-item-inspect-med"));
        else
            args.PushMarkup(Loc.GetString("space-glue-on-item-inspect-high"));
    }
}
