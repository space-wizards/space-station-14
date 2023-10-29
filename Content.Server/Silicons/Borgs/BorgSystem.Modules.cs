using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    public void InitializeModules()
    {
        SubscribeLocalEvent<BorgModuleComponent, EntGotInsertedIntoContainerMessage>(OnModuleGotInserted);
        SubscribeLocalEvent<BorgModuleComponent, EntGotRemovedFromContainerMessage>(OnModuleGotRemoved);

        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleInstalledEvent>(OnSelectableInstalled);
        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleUninstalledEvent>(OnSelectableUninstalled);
        SubscribeLocalEvent<SelectableBorgModuleComponent, BorgModuleActionSelectedEvent>(OnSelectableAction);

        SubscribeLocalEvent<ItemBorgModuleComponent, ComponentStartup>(OnProvideItemStartup);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleSelectedEvent>(OnItemModuleSelected);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleUnselectedEvent>(OnItemModuleUnselected);
    }

    private void OnModuleGotInserted(EntityUid uid, BorgModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        var chassis = args.Container.Owner;

        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp) ||
            args.Container != chassisComp.ModuleContainer ||
            !chassisComp.Activated)
            return;

        if (!_powerCell.HasDrawCharge(uid))
            return;

        InstallModule(chassis, uid, chassisComp, component);
    }

    private void OnModuleGotRemoved(EntityUid uid, BorgModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        var chassis = args.Container.Owner;

        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp) ||
            args.Container != chassisComp.ModuleContainer)
            return;

        UninstallModule(chassis, uid, chassisComp, component);
    }

    private void OnProvideItemStartup(EntityUid uid, ItemBorgModuleComponent component, ComponentStartup args)
    {
        component.ProvidedContainer = Container.EnsureContainer<Container>(uid, component.ProvidedContainerId);
    }

    private void OnSelectableInstalled(EntityUid uid, SelectableBorgModuleComponent component, ref BorgModuleInstalledEvent args)
    {
        var chassis = args.ChassisEnt;

        if (_actions.AddAction(chassis, ref component.ModuleSwapActionEntity, out var action, component.ModuleSwapActionId, uid))
        {
            action.EntityIcon = uid;
            Dirty(component.ModuleSwapActionEntity.Value, action);
        }

        if (!TryComp(chassis, out BorgChassisComponent? chassisComp))
            return;

        if (chassisComp.SelectedModule == null)
            SelectModule(chassis, uid, chassisComp, component);
    }

    private void OnSelectableUninstalled(EntityUid uid, SelectableBorgModuleComponent component, ref BorgModuleUninstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        _actions.RemoveProvidedActions(chassis, uid);
        if (!TryComp(chassis, out BorgChassisComponent? chassisComp))
            return;

        if (chassisComp.SelectedModule == uid)
            UnselectModule(chassis, chassisComp);
    }

    private void OnSelectableAction(EntityUid uid, SelectableBorgModuleComponent component, BorgModuleActionSelectedEvent args)
    {
        var chassis = args.Performer;
        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp))
            return;

        args.Handled = true;
        if (chassisComp.SelectedModule == uid)
        {
            UnselectModule(chassis, chassisComp);
            return;
        }

        SelectModule(chassis, uid, chassisComp, component);
    }

    /// <summary>
    /// Selects a module, enablind the borg to use its provided abilities.
    /// </summary>
    public void SelectModule(EntityUid chassis,
        EntityUid moduleUid,
        BorgChassisComponent? chassisComp = null,
        SelectableBorgModuleComponent? selectable = null,
        BorgModuleComponent? moduleComp = null)
    {
        if (LifeStage(chassis) >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(chassis, ref chassisComp))
            return;

        if (!Resolve(moduleUid, ref moduleComp) || !moduleComp.Installed || moduleComp.InstalledEntity != chassis)
        {
            Log.Error($"{ToPrettyString(chassis)} attempted to select uninstalled module {ToPrettyString(moduleUid)}");
            return;
        }

        if (selectable == null && !HasComp<SelectableBorgModuleComponent>(moduleUid))
        {
            Log.Error($"{ToPrettyString(chassis)} attempted to select invalid module {ToPrettyString(moduleUid)}");
            return;
        }

        if (!chassisComp.ModuleContainer.Contains(moduleUid))
        {
            Log.Error($"{ToPrettyString(chassis)} does not contain the installed module {ToPrettyString(moduleUid)}");
            return;
        }

        if (chassisComp.SelectedModule != null)
            return;

        if (chassisComp.SelectedModule == moduleUid)
            return;

        UnselectModule(chassis, chassisComp);

        var ev = new BorgModuleSelectedEvent(chassis);
        RaiseLocalEvent(moduleUid, ref ev);
        chassisComp.SelectedModule = moduleUid;
    }

    /// <summary>
    /// Unselects a module, removing its provided abilities
    /// </summary>
    public void UnselectModule(EntityUid chassis, BorgChassisComponent? chassisComp = null)
    {
        if (LifeStage(chassis) >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(chassis, ref chassisComp))
            return;

        if (chassisComp.SelectedModule == null)
            return;

        var ev = new BorgModuleUnselectedEvent(chassis);
        RaiseLocalEvent(chassisComp.SelectedModule.Value, ref ev);
        chassisComp.SelectedModule = null;
    }

    private void OnItemModuleSelected(EntityUid uid, ItemBorgModuleComponent component, ref BorgModuleSelectedEvent args)
    {
        ProvideItems(args.Chassis, uid, component: component);
    }

    private void OnItemModuleUnselected(EntityUid uid, ItemBorgModuleComponent component, ref BorgModuleUnselectedEvent args)
    {
        RemoveProvidedItems(args.Chassis, uid, component: component);
    }

    private void ProvideItems(EntityUid chassis, EntityUid uid, BorgChassisComponent? chassisComponent = null, ItemBorgModuleComponent? component = null)
    {
        if (!Resolve(chassis, ref chassisComponent) || !Resolve(uid, ref component))
            return;

        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        var xform = Transform(chassis);
        foreach (var itemProto in component.Items)
        {
            EntityUid item;

            if (!component.ItemsCreated)
            {
                item = Spawn(itemProto, xform.Coordinates);
            }
            else
            {
                item = component.ProvidedContainer.ContainedEntities
                    .FirstOrDefault(ent => Prototype(ent)?.ID == itemProto);
                if (!item.IsValid())
                {
                    Log.Debug($"no items found: {component.ProvidedContainer.ContainedEntities.Count}");
                    continue;
                }

                component.ProvidedContainer.Remove(item, EntityManager, force: true);
            }

            if (!item.IsValid())
            {
                Log.Debug("no valid item");
                continue;
            }

            var handId = $"{uid}-item{component.HandCounter}";
            component.HandCounter++;
            _hands.AddHand(chassis, handId, HandLocation.Middle, hands);
            _hands.DoPickup(chassis, hands.Hands[handId], item, hands);
            EnsureComp<UnremoveableComponent>(item);
            component.ProvidedItems.Add(handId, item);
        }

        component.ItemsCreated = true;
    }

    private void RemoveProvidedItems(EntityUid chassis, EntityUid uid, BorgChassisComponent? chassisComponent = null, ItemBorgModuleComponent? component = null)
    {
        if (!Resolve(chassis, ref chassisComponent) || !Resolve(uid, ref component))
            return;

        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        if (TerminatingOrDeleted(uid))
        {
            foreach (var (hand, item) in component.ProvidedItems)
            {
                QueueDel(item);
                _hands.RemoveHand(chassis, hand, hands);
            }
            component.ProvidedItems.Clear();
            return;
        }

        foreach (var (handId, item) in component.ProvidedItems)
        {
            if (LifeStage(item) <= EntityLifeStage.MapInitialized)
            {
                RemComp<UnremoveableComponent>(item);
                component.ProvidedContainer.Insert(item, EntityManager);
            }
            _hands.RemoveHand(chassis, handId, hands);
        }
        component.ProvidedItems.Clear();
    }

    /// <summary>
    /// Checks if a given module can be inserted into a borg
    /// </summary>
    public bool CanInsertModule(EntityUid uid, EntityUid module, BorgChassisComponent? component = null, BorgModuleComponent? moduleComponent = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(module, ref moduleComponent))
            return false;

        if (component.ModuleContainer.ContainedEntities.Count >= component.MaxModules)
        {
            if (user != null)
                Popup.PopupEntity(Loc.GetString("borg-module-too-many"), uid, user.Value);
            return false;
        }

        if (component.ModuleWhitelist?.IsValid(module, EntityManager) == false)
        {
            if (user != null)
                Popup.PopupEntity(Loc.GetString("borg-module-whitelist-deny"), uid, user.Value);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Installs and activates all modules currently inside the borg's module container
    /// </summary>
    public void InstallAllModules(EntityUid uid, BorgChassisComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var query = GetEntityQuery<BorgModuleComponent>();
        foreach (var moduleEnt in new List<EntityUid>(component.ModuleContainer.ContainedEntities))
        {
            if (!query.TryGetComponent(moduleEnt, out var moduleComp))
                continue;

            InstallModule(uid, moduleEnt, component, moduleComp);
        }
    }

    /// <summary>
    /// Deactivates all modules currently inside the borg's module container
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void DisableAllModules(EntityUid uid, BorgChassisComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var query = GetEntityQuery<BorgModuleComponent>();
        foreach (var moduleEnt in new List<EntityUid>(component.ModuleContainer.ContainedEntities))
        {
            if (!query.TryGetComponent(moduleEnt, out var moduleComp))
                continue;

            UninstallModule(uid, moduleEnt, component, moduleComp);
        }
    }

    /// <summary>
    /// Installs a single module into a borg.
    /// </summary>
    public void InstallModule(EntityUid uid, EntityUid module, BorgChassisComponent? component, BorgModuleComponent? moduleComponent = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(module, ref moduleComponent))
            return;

        if (moduleComponent.Installed)
            return;

        moduleComponent.InstalledEntity = uid;
        var ev = new BorgModuleInstalledEvent(uid);
        RaiseLocalEvent(module, ref ev);
    }

    /// <summary>
    /// Uninstalls a single module from a borg.
    /// </summary>
    public void UninstallModule(EntityUid uid, EntityUid module, BorgChassisComponent? component, BorgModuleComponent? moduleComponent = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(module, ref moduleComponent))
            return;

        if (!moduleComponent.Installed)
            return;

        moduleComponent.InstalledEntity = null;
        var ev = new BorgModuleUninstalledEvent(uid);
        RaiseLocalEvent(module, ref ev);
    }
}
