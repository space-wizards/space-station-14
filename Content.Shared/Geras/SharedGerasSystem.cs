using Content.Shared.Actions;

namespace Content.Shared.Geras;

/// <summary>
/// Geras is the god of old age, and A geras is the small morph of a slime. This system allows the slimes to have the morphing action.
/// </summary>
public abstract class SharedGerasSystem : EntitySystem
{

}

public sealed partial class MorphIntoGeras : InstantActionEvent
{

}
