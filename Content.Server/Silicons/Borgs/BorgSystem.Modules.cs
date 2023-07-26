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

        SubscribeLocalEvent<ProvideItemBorgModuleComponent, ComponentStartup>(OnProvideItemStartup);
        SubscribeLocalEvent<ProvideItemBorgModuleComponent, BorgModuleInstalledEvent>(OnProvideItemInstalled);
        SubscribeLocalEvent<ProvideItemBorgModuleComponent, BorgModuleUninstalledEvent>(OnProvideItemUninstalled);
        SubscribeLocalEvent<ProvideItemBorgModuleComponent, SwapItemBorgModuleEvent>(OnSwapItemBorgModule);
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

        EnableModule(chassis, uid, chassisComp, component);
    }

    private void OnModuleGotRemoved(EntityUid uid, BorgModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        var chassis = args.Container.Owner;

        if (Terminating(chassis))
            return;

        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp) ||
            args.Container != chassisComp.ModuleContainer)
            return;

        DisableModule(chassis, uid, chassisComp, component);
    }

    private void OnProvideItemStartup(EntityUid uid, ProvideItemBorgModuleComponent component, ComponentStartup args)
    {
        component.ProvidedContainer = Container.EnsureContainer<Container>(uid, component.ProvidedContainerId);
    }

    private void OnProvideItemInstalled(EntityUid uid, ProvideItemBorgModuleComponent component, ref BorgModuleInstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        component.ModuleSwapAction.EntityIcon = uid;
        _actions.AddAction(chassis, component.ModuleSwapAction, uid);

        if (TryComp<BorgChassisComponent>(chassis, out var chassisComp) && chassisComp.CurrentProviderModule == null)
        {
            ProvideItems(chassis, uid, chassisComp, component);
        }
    }

    private void OnProvideItemUninstalled(EntityUid uid, ProvideItemBorgModuleComponent component, ref BorgModuleUninstalledEvent args)
    {
        var chassis = args.ChassisEnt;
        _actions.RemoveProvidedActions(chassis, uid);

        if (TryComp<BorgChassisComponent>(chassis, out var chassisComp) && chassisComp.CurrentProviderModule == uid)
        {
            RemoveProvidedItems(chassis, uid, chassisComp, component);
        }
    }

    private void OnSwapItemBorgModule(EntityUid uid, ProvideItemBorgModuleComponent component, SwapItemBorgModuleEvent args)
    {
        var chassis = args.Performer;
        if (!TryComp<BorgChassisComponent>(chassis, out var chassisComp))
            return;

        if (chassisComp.CurrentProviderModule == uid)
            return;

        if (chassisComp.CurrentProviderModule != null)
            RemoveProvidedItems(chassis, chassisComp.CurrentProviderModule.Value, chassisComp);

        ProvideItems(chassis, uid, chassisComp, component);
        args.Handled = true;
    }

    private void ProvideItems(EntityUid chassis, EntityUid uid, BorgChassisComponent? chassisComponent = null, ProvideItemBorgModuleComponent? component = null)
    {
        if (Terminating(chassis))
            return;

        if (!Resolve(chassis, ref chassisComponent) || !Resolve(uid, ref component))
            return;

        if (chassisComponent.CurrentProviderModule != null)
            return;

        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        var xform = Transform(chassis);
        chassisComponent.CurrentProviderModule = uid;
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

            var handId = $"{uid}-{component.HandCounter}";
            component.HandCounter++;
            _hands.AddHand(chassis, handId, HandLocation.Middle, hands);
            _hands.DoPickup(chassis, hands.Hands[handId], item, hands);
            EnsureComp<UnremoveableComponent>(item);
            component.ProvidedItems.Add(handId, item);
        }

        component.ItemsCreated = true;
    }

    private void RemoveProvidedItems(EntityUid chassis, EntityUid uid, BorgChassisComponent? chassisComponent = null, ProvideItemBorgModuleComponent? component = null)
    {
        if (Terminating(chassis))
            return;

        if (!Resolve(chassis, ref chassisComponent) || !Resolve(uid, ref component))
            return;

        if (chassisComponent.CurrentProviderModule != uid)
            return;

        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        chassisComponent.CurrentProviderModule = null;
        foreach (var (handId, item) in component.ProvidedItems)
        {
            RemComp<UnremoveableComponent>(item);
            component.ProvidedContainer.Insert(item, EntityManager);
            _hands.RemoveHand(chassis, handId, hands);
        }
        component.ProvidedItems.Clear();
    }

    public bool CanInsertModule(EntityUid uid, EntityUid module, BorgChassisComponent? component = null, BorgModuleComponent? moduleComponent = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(module, ref moduleComponent))
            return false;

        if (component.ModuleContainer.ContainedEntities.Count >= component.MaxModules)
            return false;

        if (component.ModuleWhitelist?.IsValid(module, EntityManager) == false)
            return false;

        return true;
    }

    public void EnableAllModules(EntityUid uid, BorgChassisComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var query = GetEntityQuery<BorgModuleComponent>();
        foreach (var moduleEnt in new List<EntityUid>(component.ModuleContainer.ContainedEntities))
        {
            if (!query.TryGetComponent(moduleEnt, out var moduleComp))
                continue;

            EnableModule(uid, moduleEnt, component, moduleComp);
        }
    }

    public void DisableAllModules(EntityUid uid, BorgChassisComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var query = GetEntityQuery<BorgModuleComponent>();
        foreach (var moduleEnt in new List<EntityUid>(component.ModuleContainer.ContainedEntities))
        {
            if (!query.TryGetComponent(moduleEnt, out var moduleComp))
                continue;

            DisableModule(uid, moduleEnt, component, moduleComp);
        }
    }

    public void EnableModule(EntityUid uid, EntityUid module, BorgChassisComponent? component, BorgModuleComponent? moduleComponent = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(module, ref moduleComponent))
            return;

        if (moduleComponent.Installed)
            return;

        moduleComponent.InstalledEntity = uid;
        var ev = new BorgModuleInstalledEvent(uid);
        RaiseLocalEvent(module, ref ev);
    }

    public void DisableModule(EntityUid uid, EntityUid module, BorgChassisComponent? component, BorgModuleComponent? moduleComponent = null)
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
