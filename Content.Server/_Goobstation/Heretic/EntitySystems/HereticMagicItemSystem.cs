using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Heretic;
using Content.Shared.Inventory;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticMagicItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticMagicItemComponent, CheckMagicItemEvent>(OnCheckMagicItem);
        SubscribeLocalEvent<HereticMagicItemComponent, HeldRelayedEvent<CheckMagicItemEvent>>(OnCheckMagicItem);
        SubscribeLocalEvent<HereticMagicItemComponent, InventoryRelayedEvent<CheckMagicItemEvent>>(OnCheckMagicItem);
        SubscribeLocalEvent<HereticMagicItemComponent, ExaminedEvent>(OnMagicItemExamine);
    }

    private void OnCheckMagicItem(Entity<HereticMagicItemComponent> ent, ref CheckMagicItemEvent args)
        => args.Handled = true;
    private void OnCheckMagicItem(Entity<HereticMagicItemComponent> ent, ref HeldRelayedEvent<CheckMagicItemEvent> args)
        => args.Args.Handled = true;
    private void OnCheckMagicItem(Entity<HereticMagicItemComponent> ent, ref InventoryRelayedEvent<CheckMagicItemEvent> args)
        => args.Args.Handled = true;

    private void OnMagicItemExamine(Entity<HereticMagicItemComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<HereticComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("heretic-magicitem-examine"));
    }
}
