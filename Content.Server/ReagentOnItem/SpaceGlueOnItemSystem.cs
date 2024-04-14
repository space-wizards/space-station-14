using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Glue;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Examine;

namespace Content.Server.ReagentOnItem;

public sealed class SpaceGlueOnItemSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
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
            {
                continue;
            }

            if (!SetNextCheck(glue, uid))
            {
                // TODO: Show popup indicating the glue dried.
                RemCompDeferred<UnremoveableComponent>(uid);
                RemCompDeferred<GluedComponent>(uid);
            }
        }
    }

    private void OnHandPickUp(Entity<SpaceGlueOnItemComponent> entity, ref GotEquippedHandEvent args)
    {
        _inventory.TryGetSlotEntity(args.User, "gloves", out var gloves);

        if (HasComp<NonStickSurfaceComponent>(gloves))
        {
            return;
        }

        if (SetNextCheck(entity, entity))
        {
            _popup.PopupEntity("YOU GOT GLUED!", args.User, args.User, PopupType.MediumCaution);
        }

    }

    private bool SetNextCheck(SpaceGlueOnItemComponent glueComp, EntityUid uid)
    {
        if (glueComp.AmountOfReagentLeft < 1)
        {
            return false;
        }

        var unremoveComp = EnsureComp<UnremoveableComponent>(uid);
        unremoveComp.DeleteOnDrop = false;

        glueComp.TimeOfNextCheck = _timing.CurTime + glueComp.DurationPerUnit;
        glueComp.AmountOfReagentLeft--;

        return true;
    }

    private void OnExamine(EntityUid uid, SpaceGlueOnItemComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushMarkup("[color=white]Looks very sticky...[/color]");
        }
    }
}
