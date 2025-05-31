using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Devour;

/// <summary>
/// Used to mark a victim of a changeling as a husk, making them unrevivable and unable to be identified.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedChangelingDevourSystem))]
public sealed partial class ChangelingHuskedCorpseComponent : Component;

