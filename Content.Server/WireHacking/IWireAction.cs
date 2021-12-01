using Robust.Shared.GameObjects;

namespace Content.Server.Wires;

// a replacement to all the repetitive wire fuckery
// hidden in all components that used it:
//
// this is basically a set of things that occur when
// a wire tied to a shared 'type' (e.g., power) is
// activated/acted on
//
// takes the entity that was used, and the entity of
// the user (to be sent into the relevant systems
// that use it)
//
// for example, cutting a wire could toggle power
// on the used entity, but if the user doesn't have
// the correct protective equipment, then they should
// recieve a shock and the entire thing is cancelled
//
// all of these return a bool to indicate if the
// given action was actually performed or not
//
// this can also be serialized in YAML hopefully,
// avoiding any further issues with repetitive code
//
// this should be expected to be flyweighted;
// do NOT store per-entity state inside any class that
// inherits IWireAction (prefer changing state inside
// components instead)
public interface IWireAction
{
    // This is so that things outside WiresSystem can
    // easily grab a wire's type via an identifier,
    // rather than iterating over a list of wires
    // while checking the type of action inside.
    public object Identifier { get; }

    public void Initialize(EntityUid uid, Wire wire);

    public bool Cut(EntityUid used, EntityUid user, Wire wire);

    public bool Mend(EntityUid used, EntityUid user, Wire wire);

    public bool Pulse(EntityUid used, EntityUid user, Wire wire);
}
