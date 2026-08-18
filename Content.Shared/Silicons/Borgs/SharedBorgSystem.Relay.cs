using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<BorgChassisComponent, DamageModifyEvent>(RelayToModule);

        // By-Ref events
        SubscribeLocalEvent<BorgChassisComponent, BorgModuleInsertAttemptEvent>(RelayRefToModule);
        SubscribeLocalEvent<BorgChassisComponent, ProjectileReflectAttemptEvent>(RelayRefToModule);
    }

    protected void RelayToModule<T>(EntityUid uid, BorgChassisComponent component, T args)
        where T : EntityEventArgs, IBorgModuleRelayedEvent
    {
        var ev = new BorgModuleRelayedEvent<T>(args);

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            if (!args.RelayWhenNotInstalled && !Comp<BorgModuleComponent>(module).Installed)
                continue;

            RaiseLocalEvent(module, ref ev);
        }
    }

    protected void RelayRefToModule<T>(EntityUid uid, BorgChassisComponent component, ref T args)
        where T : IBorgModuleRelayedEvent
    {
        var ev = new BorgModuleRelayedEvent<T>(args);

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            if (!args.RelayWhenNotInstalled && !Comp<BorgModuleComponent>(module).Installed)
                continue;

            RaiseLocalEvent(module, ref ev);
            args = ev.Args;
        }
    }
}

/// <summary>
/// Relay event for borg modules
/// </summary>
[ByRefEvent]
public record struct BorgModuleRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}

/// <summary>
/// Add this to relay events if you want them to be able to be relayed to borgs modules.
/// </summary>
public interface IBorgModuleRelayedEvent
{
    /// <summary>
    /// Should it relay to modules that are not installed? If true, will relay to any modules in the borg if they are
    /// on or off. This means if the borg is out of battery, the relay will still go off on all modules true.
    /// </summary>
    bool RelayWhenNotInstalled { get; }
}
