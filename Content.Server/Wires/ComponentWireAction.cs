using Content.Shared.Wires;

namespace Content.Server.Wires;

/// <summary>
///     convenience class for wires that depend on the existence of some component to function. Slightly reduces boilerplate.
/// </summary>
public abstract class ComponentWireAction<TComponent> : BaseWireAction where TComponent : Component
{
    public abstract StatusLightState? GetLightState(Wire wire, TComponent component);
    public override StatusLightState? GetLightState(Wire wire)
    {
        return EntityManager.TryGetComponent(wire.Owner, out TComponent? component)
            ? GetLightState(wire, component)
            : StatusLightState.Off;
    }

    public virtual bool Cut(EntityUid user, Wire wire, TComponent component)
        => false;
    public virtual bool Mend(EntityUid user, Wire wire, TComponent component)
        => false;
    public virtual bool Pulse(EntityUid user, Wire wire, TComponent component)
        => false;

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        return EntityManager.TryGetComponent(wire.Owner, out TComponent? component) && Cut(user, wire, component);
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        return EntityManager.TryGetComponent(wire.Owner, out TComponent? component) && Mend(user, wire, component);
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        return EntityManager.TryGetComponent(wire.Owner, out TComponent? component) && Pulse(user, wire, component);
    }
}
