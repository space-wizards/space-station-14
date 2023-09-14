using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

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
        component.ModuleSwapAction.EntityIcon = uid;
        _actions.AddAction(chassis, component.ModuleSwapAction, uid);
        SelectModule(chassis, uid, moduleComp: component);
    }

    private void OnSelectableUninstalled(EntityUid uid, SelectableBorgModuleComponent component, ref BorgModuleUninstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        _actions.RemoveProvidedActions(chassis, uid);
        UnselectModule(chassis, uid, moduleComp: component);
    }

    private void OnSelectableAction(EntityUid uid, SelectableBorgModuleComponent component, BorgModuleActionSelectedEvent args)
    {
        var chassis = args.Performer;
        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp))
            return;

        if (chassisComp.SelectedModule == uid)
        {
            UnselectModule(chassis, chassisComp.SelectedModule, chassisComp);
            args.Handled = true;
            return;
        }

        UnselectModule(chassis, chassisComp.SelectedModule, chassisComp);
        SelectModule(chassis, uid, chassisComp, component);
        args.Handled = true;
    }

    /// <summary>
    /// Selects a module, enablind the borg to use its provided abilities.
    /// </summary>
    public void SelectModule(EntityUid chassis,
        EntityUid moduleUid,
        BorgChassisComponent? chassisComp = null,
        SelectableBorgModuleComponent? moduleComp = null)
    {
        if (Terminating(chassis) || Deleted(chassis))
            return;

        if (!Resolve(chassis, ref chassisComp))
            return;

        if (chassisComp.SelectedModule != null)
            return;

        if (chassisComp.SelectedModule == moduleUid)
            return;

        if (!Resolve(moduleUid, ref moduleComp, false))
            return;

        var ev = new BorgModuleSelectedEvent(chassis);
        RaiseLocalEvent(moduleUid, ref ev);
        chassisComp.SelectedModule = moduleUid;
    }

    /// <summary>
    /// Unselects a module, removing its provided abilities
    /// </summary>
    public void UnselectModule(EntityUid chassis,
        EntityUid? moduleUid,
        BorgChassisComponent? chassisComp = null,
        SelectableBorgModuleComponent? moduleComp = null)
    {
        if (Terminating(chassis) || Deleted(chassis))
            return;

        if (!Resolve(chassis, ref chassisComp))
            return;

        if (moduleUid == null)
            return;

        if (chassisComp.SelectedModule != moduleUid)
            return;

        if (!Resolve(moduleUid.Value, ref moduleComp, false))
            return;

        var ev = new BorgModuleUnselectedEvent(chassis);
        RaiseLocalEvent(moduleUid.Value, ref ev);
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

        foreach (var (handId, item) in component.ProvidedItems)
        {
            if (!Deleted(item) && !Terminating(item))
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
