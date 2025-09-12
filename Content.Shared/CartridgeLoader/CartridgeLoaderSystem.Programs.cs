using System.Linq;

namespace Content.Shared.CartridgeLoader;

public abstract partial class SharedCartridgeLoaderSystem : EntitySystem
{
    public Entity<T>? TryGetProgram<T>(Entity<CartridgeLoaderComponent?> ent) where T : IComponent
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (!_container.TryGetContainer(ent, InstalledContainerId, out var container))
            return null;

        foreach (var prog in container.ContainedEntities)
        {
            if (!TryComp<T>(prog, out var program))
                continue;

            return (prog, program);
        }

        return null;
    }

    public bool HasProgram<T>(Entity<CartridgeLoaderComponent?> ent) where T : IComponent
    {
        return TryGetProgram<T>(ent).HasValue;
    }

    public IReadOnlyList<EntityUid> GetPrograms(EntityUid uid)
    {
        if (_container.TryGetContainer(uid, InstalledContainerId, out var container))
            return container.ContainedEntities;

        return Array.Empty<EntityUid>();
    }

    private bool HasProgram(EntityUid uid, EntityUid program)
    {
        return GetPrograms(uid).Contains(program);
    }

    public void ActivateProgram(Entity<CartridgeLoaderComponent> ent, EntityUid program)
    {
        if (!HasProgram(ent, program))
            return;

        if (ent.Comp.ActiveProgram is { } active)
            DeactivateProgram(ent, active);

        RaiseLocalEvent(program, new CartridgeActivatedEvent(ent));
        ent.Comp.ActiveProgram = program;
        UpdateUiState(ent.AsNullable());
    }

    public void DeactivateProgram(Entity<CartridgeLoaderComponent> ent, EntityUid program)
    {
        if (!HasProgram(ent, program) || ent.Comp.ActiveProgram != program)
            return;

        RaiseLocalEvent(program, new CartridgeDeactivatedEvent(ent));
        ent.Comp.ActiveProgram = null;
        UpdateUiState(ent.AsNullable());
    }


    /// <summary>
    /// Installs a program by its prototype
    /// </summary>
    /// <param name="loaderUid">The cartridge loader uid</param>
    /// <param name="prototype">The prototype name</param>
    /// <param name="deinstallable">Whether the program can be deinstalled or not</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>Whether installing the cartridge was successful</returns>
    public bool InstallProgram(EntityUid loaderUid, string prototype, bool deinstallable = true, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return false;

        if (!_container.TryGetContainer(loaderUid, InstalledContainerId, out var container))
            return false;

        if (container.Count >= loader.DiskSpace)
            return false;

        var ev = new ProgramInstallationAttempt(loaderUid, prototype);
        RaiseLocalEvent(ref ev);

        if (ev.Cancelled)
            return false;

        var installedProgram = Spawn(prototype);
        if (!TryComp(installedProgram, out CartridgeComponent? cartridge))
            return false;

        _container.Insert(installedProgram, container);

        UpdateCartridgeInstallationStatus(installedProgram, deinstallable ? InstallationStatus.Installed : InstallationStatus.Readonly, cartridge);
        cartridge.LoaderUid = loaderUid;

        RaiseLocalEvent(installedProgram, new CartridgeAddedEvent((loaderUid, loader)));
        UpdateUiState((loaderUid, loader));
        return true;
    }

    /// <summary>
    /// Uninstalls a program using its uid
    /// </summary>
    /// <param name="loaderUid">The cartridge loader uid</param>
    /// <param name="programUid">The uid of the program to be uninstalled</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>Whether uninstalling the program was successful</returns>
    public bool UninstallProgram(EntityUid loaderUid, EntityUid programUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return false;

        if (!GetPrograms(loaderUid).Contains(programUid))
            return false;

        if (TryComp(programUid, out CartridgeComponent? cartridge))
            cartridge.LoaderUid = null;

        if (loader.ActiveProgram == programUid)
            loader.ActiveProgram = null;

        QueueDel(programUid);
        UpdateUiState((loaderUid, loader));
        return true;
    }

    /// <summary>
    /// Installs a cartridge by spawning an invisible version of the cartridges prototype into the cartridge loaders program container program container
    /// </summary>
    /// <param name="loaderUid">The cartridge loader uid</param>
    /// <param name="cartridgeUid">The uid of the cartridge to be installed</param>
    /// <param name="loader">The cartridge loader component</param>
    /// <returns>Whether installing the cartridge was successful</returns>
    public bool InstallCartridge(EntityUid loaderUid, EntityUid cartridgeUid, CartridgeLoaderComponent? loader = default!)
    {
        if (!Resolve(loaderUid, ref loader))
            return false;

        if (!TryComp(cartridgeUid, out CartridgeComponent? loadedCartridge))
            return false;

        foreach (var program in GetPrograms(loaderUid))
        {
            if (TryComp(program, out CartridgeComponent? installedCartridge) && installedCartridge.ProgramName == loadedCartridge.ProgramName)
                return false;
        }

        //This will eventually be replaced by serializing and deserializing the cartridge to copy it when something needs
        //the data on the cartridge to carry over when installing

        // For anyone stumbling onto this: Do not do this or I will cut you.
        var prototypeId = Prototype(cartridgeUid)?.ID;
        return prototypeId != null && InstallProgram(loaderUid, prototypeId, loader: loader);
    }
}
