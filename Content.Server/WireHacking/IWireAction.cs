using Content.Shared.Wires;
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

    // This is to link the wire's status with
    // its corresponding UI key. If this is null,
    // GetStatusLightData MUST also return null,
    // otherwise nothing happens.
    public object? StatusKey { get; }

    // Called when the wire in the layout
    // is created for the first time. Ensures
    // that the referenced action has all
    // the correct system references (plus
    // other information if needed,
    // but wire actions should NOT be stateful!)
    public void Initialize();

    // Called when a wire is finally processed
    // by WiresSystem upon wire layout
    // creation. Use this to set specific details
    // about the state of the entity in question.
    //
    // If this returns false, this will convert
    // the given wire into a 'dummy' wire instead.
    public bool AddWire(Wire wire, int count);

    public bool Cut(EntityUid user, Wire wire);

    public bool Mend(EntityUid user, Wire wire);

    public bool Pulse(EntityUid user, Wire wire);

    public void Update(Wire wire);

    public StatusLightData? GetStatusLightData(Wire wire);
}
