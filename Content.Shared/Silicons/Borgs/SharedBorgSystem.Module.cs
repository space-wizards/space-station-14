using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Localizations;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    [Dependency] private readonly EntityQuery<BorgModuleComponent> _moduleQuery = default!;

    public void InitializeModule()
    {
        SubscribeLocalEvent<BorgModuleComponent, ExaminedEvent>(OnModuleExamine);
        SubscribeLocalEvent<BorgModuleWhitelistComponent, ExaminedEvent>(OnWhitelistExamine);
        SubscribeLocalEvent<BorgModuleComponent, EntGotInsertedIntoContainerMessage>(OnModuleGotInserted);
        SubscribeLocalEvent<BorgModuleComponent, EntGotRemovedFromContainerMessage>(OnModuleGotRemoved);

        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleInstalledEvent>(OnSelectableInstalled);
        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleUninstalledEvent>(OnSelectableUninstalled);
        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleActionSelectedEvent>(OnSelectableAction);

        SubscribeLocalEvent<ItemBorgModuleComponent, ComponentStartup>(OnProvideItemStartup);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleSelectedEvent>(OnItemModuleSelected);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleUnselectedEvent>(OnItemModuleUnselected);

        SubscribeLocalEvent<ComponentBorgModuleComponent, BorgModuleInstalledEvent>(OnComponentModuleInstalled);
        SubscribeLocalEvent<ComponentBorgModuleComponent, BorgModuleUninstalledEvent>(OnComponentModuleUninstalled);

        SubscribeLocalEvent<ComponentBorgModuleComponent, BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent>>(
            OnComponentModuleInstalledRelay);

        SubscribeLocalEvent<BorgModuleWhitelistComponent, BorgModuleInsertAttemptEvent>(OnCheckWhitelist);
        SubscribeLocalEvent<BorgModuleWhitelistComponent, BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent>>(
            OnCheckBlacklistRelay);
    }

    #region BorgModule
    private void OnModuleExamine(Entity<BorgModuleComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BorgModuleComponent)))
        {
            if (TryFormatList(ent.Comp.BorgFitTypes, "borg-module-fit", "types", out var list))
                args.PushMarkup(list);
        }
    }

    private void OnWhitelistExamine(Entity<BorgModuleWhitelistComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BorgModuleComponent), 1))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.WhitelistInfo));
        }
    }

    private bool TryFormatList(List<LocId>? list, string messageId, string listId, [NotNullWhen(true)] out string? formattedList)
    {
        formattedList = null;

        if (list == null || list.Count == 0)
            return false;

        var entries = ContentLocalizationManager.FormatList([.. list.Select(s => Loc.GetString(s))]);

        formattedList = Loc.GetString(messageId, (listId, entries));
        return true;

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
    #endregion

    #region SelectableBorgModule
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
    #endregion

    #region ItemBorgModule
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
    #endregion

    #region ComponentBorgModule
    private void OnComponentModuleInstalled(Entity<ComponentBorgModuleComponent> ent, ref BorgModuleInstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        EntityManager.AddComponents(chassis, ent.Comp.Components);
    }

    private void OnComponentModuleUninstalled(Entity<ComponentBorgModuleComponent> ent,
        ref BorgModuleUninstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        EntityManager.RemoveComponents(chassis, ent.Comp.Components);
    }

    private void OnComponentModuleInstalledRelay(Entity<ComponentBorgModuleComponent> ent,
        ref BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent> args)
    {
        if (args.Args.Cancelled ||
            !TryComp<ComponentBorgModuleComponent>(args.Args.ModuleEnt, out var newModule))
            return;

        foreach (var comp in newModule.Components)
        {
            if (ent.Comp.Components.TryGetComponent(comp.Key, out _))
            {
                args.Args.Cancelled = true;
                args.Args.Reason = Loc.GetString("borg-module-incompatible", ("existing", ent));
            }
        }

    }
    #endregion

    #region ModuleWhitelist

    private void OnCheckWhitelist(Entity<BorgModuleWhitelistComponent> ent, ref BorgModuleInsertAttemptEvent args)
    {
        if (args.Cancelled || !TryComp<BorgChassisComponent>(args.ChassisEnt, out var chassis))
            return;

        //loop over all other contained modules to see if any conflict with this module's blacklist
        //while simultaneously checking if any module fits its prerequisite criteria
        var prerequisiteFulfilled = false;
        foreach (var containedModuleUid in chassis.ModuleContainer.ContainedEntities)
        {
            if (_whitelist.IsWhitelistPass(ent.Comp.ModuleBlacklist, containedModuleUid))
            {
                args.Reason = Loc.GetString("borg-module-incompatible", ("existing", containedModuleUid));
                args.Cancelled = true;
                return;
            }
            if (!prerequisiteFulfilled && _whitelist.IsWhitelistPassOrNull(ent.Comp.ModuleWhitelist, containedModuleUid))
                prerequisiteFulfilled = true;
        }
        if (!prerequisiteFulfilled)
        {
            args.Reason = Loc.GetString("borg-module-prerequisite-unfulfilled");
            args.Cancelled = true;
        }
    }

    private void OnCheckBlacklistRelay(Entity<BorgModuleWhitelistComponent> ent, ref BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.ModuleBlacklist, args.Args.ModuleEnt))
        {
            args.Args.Cancelled = true;
            args.Args.Reason = Loc.GetString("borg-module-incompatible", ("existing", ent));
        }
    }

    //TODO: Replace this with a relayed event based system once there's a QueueRemove
    //or something similar implemented that defers entity removal from containers to the following tick
    //this cannot be implemented as a relayed event because the act of removing a module
    //from a chassis modifies the relay's foreach loop collection to be modified, thus throwing an error

    /// This function removes all modules who are now invalidated by the removal of removedModule
    private void ValidateWhitelists(Entity<BorgChassisComponent> chassis, EntityUid removedModule)
    {
        var toRemove = new List<EntityUid>();
        foreach (var containedModuleUid in chassis.Comp.ModuleContainer.ContainedEntities)
        {
            if (containedModuleUid == removedModule ||
                !TryComp<BorgModuleWhitelistComponent>(containedModuleUid, out var whitelist) ||
                whitelist.ModuleWhitelist == null)
                continue;

            var keep = false;

            foreach (var checkAgainstModuleUid in chassis.Comp.ModuleContainer.ContainedEntities)
            {
                if (checkAgainstModuleUid == containedModuleUid ||
                    checkAgainstModuleUid == removedModule)
                    continue;

                if (_whitelist.IsWhitelistPass(whitelist.ModuleWhitelist, checkAgainstModuleUid))
                {
                    keep = true;
                    break;
                }
            }
            if (!keep)
                toRemove.Add(containedModuleUid);
        }

        foreach (var moduleUid in toRemove)
        {
            _container.Remove(moduleUid, chassis.Comp.ModuleContainer);
        }
    }

    #endregion
}
