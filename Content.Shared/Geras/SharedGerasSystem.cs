using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Geras;

/// <summary>
/// A Geras is the small morph of a slime. This system handles exactly that.
/// </summary>
public abstract class SharedGerasSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed class GerasChildEntity : EntityEventArgs
    {
        public NetEntity ParentUid;
        public NetEntity ChildUid;
    }
}

public sealed partial class MorphIntoGeras : InstantActionEvent
{

}
