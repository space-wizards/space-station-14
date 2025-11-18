using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<BorgChassisComponent, DamageModifyEvent>(RelayToModule);
    }

    protected void RelayToModule<T>(EntityUid uid, BorgChassisComponent component, T args) where T : class
    {
        var ev = new BorgModuleRelayedEvent<T>(args);

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            RaiseLocalEvent(module, ref ev);
        }
    }

    protected void RelayRefToModule<T>(EntityUid uid, BorgChassisComponent component, ref T args) where T : class
    {
        var ev = new BorgModuleRelayedEvent<T>(args);

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            RaiseLocalEvent(module, ref ev);
        }
    }
}

[ByRefEvent]
public record struct BorgModuleRelayedEvent<TEvent>(TEvent Args)
{
    public readonly TEvent Args = Args;
}
