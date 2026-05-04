using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.UserInterface;

// im TIRED of having to make a 15 line system on the client whose SOLE purpose is to call bui.Update() with compstates
// sue me for shitcode
[Serializable, NetSerializable]
public sealed class DummyBoundUserInterfaceState : BoundUserInterfaceState;
