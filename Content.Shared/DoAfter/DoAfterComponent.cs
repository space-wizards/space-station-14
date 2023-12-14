using System.Threading.Tasks;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfterComponent : Component
{
    [DataField("nextId")]
    public ushort NextId;

    [DataField("doAfters")]
    public Dictionary<ushort, DoAfter> DoAfters = new();

    // Used by obsolete async do afters
    public readonly Dictionary<ushort, TaskCompletionSource<DoAfterStatus>> AwaitedDoAfters = new();
}

[Serializable, NetSerializable]
public sealed class DoAfterComponentState : ComponentState
{
    public readonly ushort NextId;
    public readonly Dictionary<ushort, DoAfter> DoAfters;

    public DoAfterComponentState(IEntityManager entManager, DoAfterComponent component)
    {
        NextId = component.NextId;

        // Cursed test bugs - See CraftingTests.CancelCraft
        // The following is wrapped in an if DEBUG. This is tests don't (de)serialize net messages and just copy objects
        // by reference. This means that the server will directly modify cached server states on the client's end.
        // Crude fix at the moment is to used modified state handling while in debug mode Otherwise, this test cannot work.
#if !DEBUG
        DoAfters = component.DoAfters;
#else
        DoAfters = new();
        foreach (var (id, doAfter) in component.DoAfters)
        {
            var newDoAfter = new DoAfter(entManager, doAfter);
            DoAfters.Add(id, newDoAfter);
        }
#endif
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
