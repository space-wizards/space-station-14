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
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpaceGlueOnItemComponent, UnremoveableComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var glue, out var _, out var meta))
        {
            if (_timing.CurTime < glue.Until)
                continue;

            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
        }
    }

    private void OnHandPickUp(Entity<SpaceGlueOnItemComponent> entity, ref GotEquippedHandEvent args)
    {
        _inventory.TryGetSlotEntity(args.User, "gloves", out var gloves);

        if (HasComp<NonAdhesiveSurfaceComponent>(gloves))
        {
            return;
        }
        var comp = EnsureComp<UnremoveableComponent>(entity);
        comp.DeleteOnDrop = false;
        entity.Comp.Until = _timing.CurTime + entity.Comp.Duration;
        _popup.PopupEntity("YOU GOT GLUED!", args.User, args.User, PopupType.MediumCaution);

    }
}
