using Content.Shared.Actions;
using Content.Shared.Cuffs;
using Content.Shared.DeadSpace.Abilities.ChainAbility;
using Content.Server.DeadSpace.Abilities.ChainAbility.Components;
using Content.Shared.DoAfter;
using Content.Server.Inventory;
using Content.Shared.Cuffs.Components;

namespace Content.Server.DeadSpace.Abilities.ChainAbility;

public sealed class ChainAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChainAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChainAbilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChainAbilityComponent, ChainAbilityActionEvent>(OnChain);
        SubscribeLocalEvent<ChainAbilityComponent, ChainAbilityDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, ChainAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ChainAbilityActionEntity, component.ChainAbility, uid);
    }

    private void OnShutdown(EntityUid uid, ChainAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ChainAbilityActionEntity);
    }

    private void OnChain(EntityUid uid, ChainAbilityComponent component, ChainAbilityActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        args.Handled = true;

        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.ChainDuration, new ChainAbilityDoAfterEvent(), uid, target: args.Target)
        {
            BreakOnMove = true,
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, ChainAbilityComponent component, ChainAbilityDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        var handcuff = Spawn(component.HandcuffsProtorype, Transform(uid).Coordinates);

        if (!TryComp<HandcuffComponent>(handcuff, out var handcuffComponent) || !handcuffComponent.Used)
        {
            Del(handcuff);
            return;
        }

        if (!_cuffs.TryAddNewCuffs(target, target, handcuff))
            Del(handcuff);


        if (component.NeedBoots)
        {
            Equip(target, component.BootsProtorype, "shoes");
        }

        if (component.NeedMuzzle)
        {
            Equip(target, component.MaskMuzzleProtorype, "mask");
        }

        if (component.NeedBandage)
        {
            Equip(target, component.BandageProtorype, "eyes");
        }
    }

    private void Equip(EntityUid uid, string proto, string slot)
    {
        var equip = Spawn(proto, Transform(uid).Coordinates);

        _inventory.TryUnequip(uid, slot, true, true);
        if (!_inventory.TryEquip(uid, equip, slot, true, true))
            Del(equip);
    }

}
