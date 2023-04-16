using System.Threading.Tasks;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoAfterSystem))]
public sealed class DoAfterComponent : Component
{
    [DataField("nextId")]
    public ushort NextId;

    [DataField("doAfters")]
    public readonly Dictionary<ushort, DoAfter> DoAfters = new();

    // Used by obsolete async do afters
    public readonly Dictionary<ushort, TaskCompletionSource<DoAfterStatus>> AwaitedDoAfters = new();
}

[Serializable, NetSerializable]
public sealed class DoAfterComponentState : ComponentState
{
    public readonly ushort NextId;
    public readonly Dictionary<ushort, DoAfter> DoAfters;

    public DoAfterComponentState(DoAfterComponent component)
    {
        NextId = component.NextId;
        DoAfters = component.DoAfters;
    }
}

[Serializable, NetSerializable]
public enum DoAfterStatus : byte
{
    Invalid,
    Running,
    Cancelled,
    Finished,
}
