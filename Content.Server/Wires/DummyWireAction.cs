using Content.Shared.Wires;

namespace Content.Server.Wires;

// Exists so that dummy wires can be added.
//
// You *shouldn't* be adding these as raw
// wire actions, but it's here anyways as
// a serializable class for consistency.
// C'est la vie.
public sealed class DummyWireAction : BaseWireAction
{
    public override Color Color { get; set; } = Color.White;
    public override string Name { get; set; } = "";

    public override object? StatusKey { get; } = null;

    public override bool AddWire(Wire wire, int count) => true;

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        return true;
    }

    // doesn't matter if you get any information off of this,
    // if you really want to mess with dummy wires, you should
    // probably code your own implementation?
    private enum DummyWireActionIdentifier
    {
        Key,
    }
}
