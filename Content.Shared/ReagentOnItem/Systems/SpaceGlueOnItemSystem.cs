using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Content.Shared.NameModifier.EntitySystems;


namespace Content.Shared.ReagentOnItem;

public sealed class SpaceGlueOnItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceGlueOnItemComponent, GotEquippedHandEvent>(OnHandPickUp);
        SubscribeLocalEvent<SpaceGlueOnItemComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpaceGlueOnItemComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpaceGlueOnItemComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);

        SubscribeLocalEvent<SpaceGlueOnItemComponent, ComponentGetState>(GetSpaceGlueState);
        SubscribeLocalEvent<SpaceGlueOnItemComponent, ComponentHandleState>(HandleSpaceGlueState);
    }

    private void OnInit(EntityUid uid, SpaceGlueOnItemComponent component, ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(uid);
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
                _nameMod.RefreshNameModifiers(uid);
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
            _popup.PopupPredicted(Loc.GetString("space-glue-on-item-hand-stuck", ("target", Identity.Entity(entity, EntityManager))), args.User, args.User, PopupType.MediumCaution);
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

        var duration = _random.Next(glueComp.MinimumDurationPerUnit, glueComp.MaximumDurationPerUnit);

        glueComp.TimeOfNextCheck = _timing.CurTime + duration;
        glueComp.EffectStacks -= 1;

        Dirty(uid, glueComp);

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

    private void OnRefreshNameModifiers(Entity<SpaceGlueOnItemComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("glued-name-prefix");
    }

    private void HandleSpaceGlueState(EntityUid uid, SpaceGlueOnItemComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ReagentOnItemComponentState state)
            return;

        component.EffectStacks = state.EffectStacks;
        component.MaxStacks = state.MaxStacks;
    }

    private void GetSpaceGlueState(EntityUid uid, SpaceGlueOnItemComponent component, ref ComponentGetState args)
    {
        args.State = new ReagentOnItemComponentState(component.EffectStacks, component.MaxStacks);
    }
}
