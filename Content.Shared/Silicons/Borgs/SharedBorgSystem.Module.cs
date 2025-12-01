using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Localizations;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    private EntityQuery<BorgModuleComponent> _moduleQuery;

    public void InitializeModule()
    {
        SubscribeLocalEvent<BorgModuleComponent, ExaminedEvent>(OnModuleExamine);
        SubscribeLocalEvent<BorgModuleComponent, EntGotInsertedIntoContainerMessage>(OnModuleGotInserted);
        SubscribeLocalEvent<BorgModuleComponent, EntGotRemovedFromContainerMessage>(OnModuleGotRemoved);

        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleInstalledEvent>(OnSelectableInstalled);
        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleUninstalledEvent>(OnSelectableUninstalled);
        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleActionSelectedEvent>(OnSelectableAction);

        SubscribeLocalEvent<ItemBorgModuleComponent, ComponentStartup>(OnProvideItemStartup);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleSelectedEvent>(OnItemModuleSelected);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleUnselectedEvent>(OnItemModuleUnselected);

        _moduleQuery = GetEntityQuery<BorgModuleComponent>();
    }

    private void OnModuleExamine(Entity<BorgModuleComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.BorgFitTypes == null)
            return;

        if (ent.Comp.BorgFitTypes.Count == 0)
            return;

        var typeList = new List<string>();

        foreach (var type in ent.Comp.BorgFitTypes)
        {
            typeList.Add(Loc.GetString(type));
        }

        var types = ContentLocalizationManager.FormatList(typeList);
        args.PushMarkup(Loc.GetString("borg-module-fit", ("types", types)));
    }

    private void OnModuleGotInserted(Entity<BorgModuleComponent> module, ref EntGotInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        var chassis = args.Container.Owner;

        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp) ||
            args.Container != chassisComp.ModuleContainer ||
            !chassisComp.Active)
            return;

        InstallModule((chassis, chassisComp), module.AsNullable());
    }

    private void OnModuleGotRemoved(Entity<BorgModuleComponent> module, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        var chassis = args.Container.Owner;

        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp) ||
            args.Container != chassisComp.ModuleContainer)
            return;

        UninstallModule((chassis, chassisComp), module.AsNullable());
    }

    private void OnSelectableInstalled(Entity<SelectableBorgModuleComponent> module, ref BorgModuleInstalledEvent args)
    {
        var chassis = args.ChassisEnt;

        if (_actions.AddAction(chassis, ref module.Comp.ModuleSwapActionEntity, out var action, module.Comp.ModuleSwapAction, module.Owner))
        {
            Dirty(module); // for ModuleSwapActionEntity after the action has been spawned
            var actEnt = (module.Comp.ModuleSwapActionEntity.Value, action);
            _actions.SetEntityIcon(actEnt, module.Owner);
            if (TryComp<BorgModuleIconComponent>(module, out var moduleIconComp))
                _actions.SetIcon(actEnt, moduleIconComp.Icon);

            /// Set a custom name and description on the action. The borg module action prototypes are shared across
            /// all modules. Extract localized names, then populate variables with the info from the module itself.
            var moduleName = Name(module);
            var actionMetaData = MetaData(module.Comp.ModuleSwapActionEntity.Value);

            var instanceName = Loc.GetString("borg-module-action-name", ("moduleName", moduleName));
            _metaData.SetEntityName(module.Comp.ModuleSwapActionEntity.Value, instanceName, actionMetaData);
            var instanceDesc = Loc.GetString("borg-module-action-description", ("moduleName", moduleName));
            _metaData.SetEntityDescription(module.Comp.ModuleSwapActionEntity.Value, instanceDesc, actionMetaData);
        }

        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp))
            return;

        if (chassisComp.SelectedModule == null)
            SelectModule((chassis, chassisComp), module.Owner);
    }

    private void OnSelectableUninstalled(Entity<SelectableBorgModuleComponent> module, ref BorgModuleUninstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        _actions.RemoveProvidedActions(chassis, module.Owner);
        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp))
            return;

        if (chassisComp.SelectedModule == module.Owner)
            UnselectModule((chassis, chassisComp));
    }

    private void OnSelectableAction(Entity<SelectableBorgModuleComponent> module, ref BorgModuleActionSelectedEvent args)
    {
        var chassis = args.Performer;
        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp))
            return;

        var selected = chassisComp.SelectedModule;

        args.Handled = true;
        UnselectModule((chassis, chassisComp));

        if (selected != module.Owner)
        {
            SelectModule((chassis, chassisComp), module.Owner);
        }
    }

    private void OnProvideItemStartup(Entity<ItemBorgModuleComponent> module, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(module.Owner, module.Comp.HoldingContainer);
    }

    private void OnItemModuleSelected(Entity<ItemBorgModuleComponent> module, ref BorgModuleSelectedEvent args)
    {
        ProvideItems(args.Chassis, module.AsNullable());
    }

    private void OnItemModuleUnselected(Entity<ItemBorgModuleComponent> module, ref BorgModuleUnselectedEvent args)
    {
        RemoveProvidedItems(args.Chassis, module.AsNullable());
    }

    private void ProvideItems(Entity<BorgChassisComponent?> chassis, Entity<ItemBorgModuleComponent?> module)
    {
        if (!Resolve(chassis, ref chassis.Comp) || !Resolve(module, ref module.Comp))
            return;

        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        if (!_container.TryGetContainer(module, module.Comp.HoldingContainer, out var container))
            return;

        var xform = Transform(chassis);

        for (var i = 0; i < module.Comp.Hands.Count; i++)
        {
            var hand = module.Comp.Hands[i];
            var handId = $"{GetNetEntity(module.Owner)}-hand-{i}";

            _hands.AddHand((chassis.Owner, hands), handId, hand.Hand);
            EntityUid? item = null;

            if (module.Comp.Spawned)
            {
                if (module.Comp.StoredItems.TryGetValue(handId, out var storedItem))
                {
                    item = storedItem;
                    // DoPickup handles removing the item from the container.
                }
            }
            else if (hand.Item is { } itemProto)
            {
                item = PredictedSpawnAtPosition(itemProto, xform.Coordinates);
            }

            if (item is { } pickUp)
            {
                _hands.DoPickup(chassis, handId, pickUp, hands);
                if (!hand.ForceRemovable && hand.Hand.Whitelist == null && hand.Hand.Blacklist == null)
                {
                    EnsureComp<UnremoveableComponent>(pickUp);
                }
            }
        }

        module.Comp.Spawned = true;
        Dirty(module);
    }

    private void RemoveProvidedItems(Entity<BorgChassisComponent?> chassis, Entity<ItemBorgModuleComponent?> module)
    {
        if (!Resolve(chassis, ref chassis.Comp) || !Resolve(module, ref module.Comp))
            return;

        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        if (!_container.TryGetContainer(module, module.Comp.HoldingContainer, out var container))
            return;

        if (TerminatingOrDeleted(module))
            return;

        for (var i = 0; i < module.Comp.Hands.Count; i++)
        {
            var handId = $"{GetNetEntity(module.Owner)}-hand-{i}";

            if (_hands.TryGetHeldItem((chassis.Owner, hands), handId, out var held))
            {
                RemComp<UnremoveableComponent>(held.Value);
                _container.Insert(held.Value, container);
                module.Comp.StoredItems[handId] = held.Value;
            }
            else
            {
                module.Comp.StoredItems.Remove(handId);
            }

            _hands.RemoveHand((chassis.Owner, hands), handId);
        }

        Dirty(module);
    }
}
