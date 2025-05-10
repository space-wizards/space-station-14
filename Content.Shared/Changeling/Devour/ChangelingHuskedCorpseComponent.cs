using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Devour;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedChangelingDevourSystem))]
public sealed partial class ChangelingHuskedCorpseComponent : Component;

