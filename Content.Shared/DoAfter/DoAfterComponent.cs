using System.Threading.Tasks;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfterComponent : Component
{
    /// <summary>
    /// The id of the next doafter
    /// </summary>
    [DataField]
    public ushort NextId;

    /// <summary>
    /// collection of id + doafter
    /// </summary>
    [DataField]
    public Dictionary<ushort, DoAfter> DoAfters = new();

    /// <summary>
    /// What should the delay be reduced to after completion?
    /// </summary>
    [DataField]
    public TimeSpan? DelayReduction;

    // This region of fields are for setting parameters for a do after
    // Eventually DoAfterArgs should be completely merged with this class
    // Still requires some setup currently, but this way it hardcodes doafters a lot less
    #region DoAfterArgsSettings
    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.AttemptFrequency"/>
    /// </summary>
    [DataField]
    public AttemptFrequency AttemptFrequency;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Broadcast"/>
    /// </summary>
    [DataField]
    public bool Broadcast;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Delay"/>
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Hidden"/>
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// Should this DoAfter repeat after being completed?
    /// </summary>
    [DataField]
    public bool Repeat;

    #region Break/Cancellation Options
    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.NeedHand"/>
    /// </summary>
    [DataField]
    public bool NeedHand;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnHandChange"/>
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnDropItem"/>
    /// </summary>
    [DataField]
    public bool BreakOnDropItem = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnMove"/>
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnWeightlessMove"/>
    /// </summary>
    [DataField]
    public bool BreakOnWeightlessMove = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.MovementThreshold"/>
    /// </summary>
    [DataField]
    public float MovementThreshold = 0.3f;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.DistanceThreshold"/>
    /// </summary>
    [DataField]
    public float? DistanceThreshold;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnDamage"/>
    /// </summary>
    [DataField]
    public bool BreakOnDamage;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.DamageThreshold"/>
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 1;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.RequireCanInteract"/>
    /// </summary>
    [DataField]
    public bool RequireCanInteract = true;
    // End Break/Cancellation Options
    #endregion

    // End DoAfterArgsSettings
    #endregion

    // Used by obsolete async do afters
    public readonly Dictionary<ushort, TaskCompletionSource<DoAfterStatus>> AwaitedDoAfters = new();
}

[Serializable, NetSerializable]
public sealed class DoAfterComponentState : ComponentState
{
    public readonly ushort NextId;
    public readonly Dictionary<ushort, DoAfter> DoAfters;
    public readonly TimeSpan? DelayReduction;

    public DoAfterComponentState(IEntityManager entManager, DoAfterComponent component)
    {
        NextId = component.NextId;
        DelayReduction = component.DelayReduction;

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
