using Content.Shared.Actions;

namespace Content.Shared.Geras;

/// <summary>
/// A Geras is the small morph of a slime. This system handles exactly that.
/// </summary>
public abstract class SharedGerasSystem : EntitySystem
{

}

public sealed partial class MorphIntoGeras : InstantActionEvent
{

}
