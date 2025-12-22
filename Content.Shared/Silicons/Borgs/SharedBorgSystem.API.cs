using System.Linq;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    /// <summary>
    /// Can this borg currently activate it's <see cref="ItemToggleComponent"/>?
    /// The requirements for this are
    /// - Having enough power in its power cell
    /// - Having a player mind attached
    /// - The borg is alive (not crit or dead).
    /// </summary>
    public bool CanActivate(Entity<BorgChassisComponent> chassis)
    {
        if (!_powerCell.HasDrawCharge(chassis.Owner))
            return false;

        // TODO: Replace this with something else, only the client's own mind is networked to them,
        // so this will always be false for the minds of other clients.
        if (!_mind.TryGetMind(chassis.Owner, out _, out _))
            return false;

        if (_mobState.IsIncapacitated(chassis.Owner))
            return false;

        return true;
    }

    /// <summary>
    /// Activates the borg if the conditions are met.
    /// Returns true if the borg was activated.
    /// </summary>
    public bool TryActivate(Entity<BorgChassisComponent> chassis, EntityUid? user = null)
    {
        if (chassis.Comp.Active)
            return false; // Already active.

        if (!CanActivate(chassis))
            return false;

        SetActive(chassis, true, user);
        return true;

    }

    /// <summary>
    /// Activates or deactivates a borg.
    /// If active the borg
    /// - can use modules and
    /// - has full movement speed.
    /// </summary>
    public void SetActive(Entity<BorgChassisComponent> chassis, bool active, EntityUid? user = null)
    {
        if (chassis.Comp.Active == active)
            return;

        chassis.Comp.Active = active;
        Dirty(chassis);

        if (active)
            InstallAllModules(chassis.AsNullable());
        else
            DisableAllModules(chassis.AsNullable());

        _powerCell.SetDrawEnabled(chassis.Owner, active);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(chassis);

        var sound = active ? chassis.Comp.ActivateSound : chassis.Comp.DeactivateSound;
        // If a user is given predict the audio for them, if not keep it unpredicted.
        if (user != null)
            _audio.PlayPredicted(sound, chassis.Owner, user);
        else if (_net.IsServer)
            _audio.PlayPvs(sound, chassis.Owner);
    }

    /// <summary>
    /// Inserts a new module into a borg, the same as if a player inserted it manually.
    /// This does not run checks to see if the borg is actually allowed to be inserted, such as whitelists.
    /// </summary>
    /// <param name="ent">The borg to insert into.</param>
    /// <param name="module">The module to insert.</param>
    public void InsertModule(Entity<BorgChassisComponent> ent, EntityUid module)
    {
        _container.Insert(module, ent.Comp.ModuleContainer);
    }

    /// <summary>
    /// Sets <see cref="BorgChassisComponent.ModuleWhitelist"/>.
    /// </summary>
    /// <param name="ent">The borg to modify.</param>
    /// <param name="whitelist">The new module whitelist.</param>
    public void SetModuleWhitelist(Entity<BorgChassisComponent> ent, EntityWhitelist? whitelist)
    {
        ent.Comp.ModuleWhitelist = whitelist;
        Dirty(ent);
    }

    /// <summary>
    /// Sets <see cref="BorgChassisComponent.MaxModules"/>.
    /// </summary>
    /// <param name="ent">The borg to modify.</param>
    /// <param name="maxModules">The new max module count.</param>
    public void SetMaxModules(Entity<BorgChassisComponent> ent, int maxModules)
    {
        ent.Comp.MaxModules = maxModules;
        Dirty(ent);
    }

    /// <summary>
    /// Checks that a player has fulfilled the requirements for the borg job, i.e. they are not banned from that role.
    /// Always true on the client.
    /// </summary>
    /// <remarks>
    /// TODO: This currently causes mispredicts, but we have no way of knowing on the client if a player is banned.
    /// Maybe solve this by giving banned players an unborgable trait instead?
    /// </remarks>
    public virtual bool CanPlayerBeBorged(ICommonSession session)
    {
        return true;
    }

    /// <summary>
    /// Installs a single module into a borg.
    /// </summary>
    public void InstallModule(Entity<BorgChassisComponent?> borg, Entity<BorgModuleComponent?> module)
    {
        if (!Resolve(borg, ref borg.Comp) || !Resolve(module, ref module.Comp))
            return;

        if (module.Comp.Installed)
            return;

        module.Comp.InstalledEntity = borg.Owner;
        Dirty(module);
        var ev = new BorgModuleInstalledEvent(borg.Owner);
        RaiseLocalEvent(module, ref ev);
    }

    /// <summary>
    /// Uninstalls a single module from a borg.
    /// </summary>
    public void UninstallModule(Entity<BorgChassisComponent?> borg, Entity<BorgModuleComponent?> module)
    {
        if (!Resolve(borg, ref borg.Comp) || !Resolve(module, ref module.Comp))
            return;

        if (!module.Comp.Installed)
            return;

        module.Comp.InstalledEntity = null;
        Dirty(module);
        var ev = new BorgModuleUninstalledEvent(borg.Owner);
        RaiseLocalEvent(module, ref ev);
    }

    /// <summary>
    /// Installs and activates all modules currently inside the borg's module container.
    /// </summary>
    public void InstallAllModules(Entity<BorgChassisComponent?> borg)
    {
        if (!Resolve(borg, ref borg.Comp))
            return;

        foreach (var moduleEnt in new List<EntityUid>(borg.Comp.ModuleContainer.ContainedEntities))
        {
            if (!_moduleQuery.TryGetComponent(moduleEnt, out var moduleComp))
                continue;

            InstallModule(borg, (moduleEnt, moduleComp));
        }
    }

    /// <summary>
    /// Deactivates all modules currently inside the borg's module container.
    /// </summary>
    public void DisableAllModules(Entity<BorgChassisComponent?> borg)
    {
        if (!Resolve(borg, ref borg.Comp))
            return;

        foreach (var moduleEnt in new List<EntityUid>(borg.Comp.ModuleContainer.ContainedEntities))
        {
            if (!_moduleQuery.TryGetComponent(moduleEnt, out var moduleComp))
                continue;

            UninstallModule(borg, (moduleEnt, moduleComp));
        }
    }

    /// <summary>
    /// Sets <see cref="BorgModuleComponent.DefaultModule"/>.
    /// </summary>
    public void SetBorgModuleDefault(Entity<BorgModuleComponent> ent, bool newDefault)
    {
        ent.Comp.DefaultModule = newDefault;
        Dirty(ent);
    }

    /// <summary>
    /// Checks if a given module can be inserted into a borg.
    /// </summary>
    public bool CanInsertModule(Entity<BorgChassisComponent?> chassis, Entity<BorgModuleComponent?> module, EntityUid? user = null)
    {
        if (!Resolve(chassis, ref chassis.Comp) || !Resolve(module, ref module.Comp))
            return false;

        if (chassis.Comp.ModuleContainer.ContainedEntities.Count >= chassis.Comp.MaxModules)
        {
            _popup.PopupClient(Loc.GetString("borg-module-too-many"), chassis.Owner, user);
            return false;
        }

        if (_whitelist.IsWhitelistFail(chassis.Comp.ModuleWhitelist, module))
        {
            _popup.PopupClient(Loc.GetString("borg-module-whitelist-deny"), chassis.Owner, user);
            return false;
        }

        if (TryComp<ItemBorgModuleComponent>(module, out var itemModuleComp))
        {
            foreach (var containedModuleUid in chassis.Comp.ModuleContainer.ContainedEntities)
            {
                if (!TryComp<ItemBorgModuleComponent>(containedModuleUid, out var containedItemModuleComp))
                    continue;

                if (containedItemModuleComp.Hands.Count == itemModuleComp.Hands.Count &&
                    containedItemModuleComp.Hands.All(itemModuleComp.Hands.Contains))
                {
                    _popup.PopupClient(Loc.GetString("borg-module-duplicate"), chassis.Owner, user);
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a module can be removed from a borg.
    /// </summary>
    /// <param name="module">The module to remove from the borg.</param>
    /// <returns>True if the module can be removed.</returns>
    public bool CanRemoveModule(Entity<BorgModuleComponent> module)
    {
        if (module.Comp.DefaultModule)
            return false;

        return true;
    }

    /// <summary>
    /// Selects a module, enabling the borg to use its provided abilities.
    /// </summary>
    public void SelectModule(Entity<BorgChassisComponent?> chassis,
        Entity<BorgModuleComponent?> module)
    {
        if (LifeStage(chassis) >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(chassis, ref chassis.Comp))
            return;

        if (!Resolve(module, ref module.Comp) || !module.Comp.Installed || module.Comp.InstalledEntity != chassis.Owner)
        {
            Log.Error($"{ToPrettyString(chassis)} attempted to select uninstalled module {ToPrettyString(module)}");
            return;
        }

        if (!HasComp<SelectableBorgModuleComponent>(module))
        {
            Log.Error($"{ToPrettyString(chassis)} attempted to select invalid module {ToPrettyString(module)}");
            return;
        }

        if (!chassis.Comp.ModuleContainer.Contains(module))
        {
            Log.Error($"{ToPrettyString(chassis)} does not contain the installed module {ToPrettyString(module)}");
            return;
        }

        if (chassis.Comp.SelectedModule == module.Owner)
            return;

        UnselectModule(chassis);

        var ev = new BorgModuleSelectedEvent(chassis);
        RaiseLocalEvent(module, ref ev);
        chassis.Comp.SelectedModule = module.Owner;
        Dirty(chassis);
    }

    /// <summary>
    /// Unselects a module, removing its provided abilities.
    /// </summary>
    public void UnselectModule(Entity<BorgChassisComponent?> chassis)
    {
        if (LifeStage(chassis) >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(chassis, ref chassis.Comp))
            return;

        if (chassis.Comp.SelectedModule == null)
            return;

        var ev = new BorgModuleUnselectedEvent(chassis);
        RaiseLocalEvent(chassis.Comp.SelectedModule.Value, ref ev);
        chassis.Comp.SelectedModule = null;
        Dirty(chassis);
    }
}
