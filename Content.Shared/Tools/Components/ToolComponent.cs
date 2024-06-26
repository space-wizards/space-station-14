using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedToolSystem))]
public sealed partial class ToolComponent : Component
{
    [DataField]
    public PrototypeFlags<ToolQualityPrototype> Qualities  = [];

    /// <summary>
    ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
    /// </summary>
    [DataField]
    public float SpeedModifier  = 1;

    [DataField]
    public SoundSpecifier? UseSound;
}

/// <summary>
/// Attempt event called *before* any do afters to see if the tool usage should succeed or not.
/// Raised on both the tool and then target.
/// </summary>
public sealed class ToolUseAttemptEvent(EntityUid user, float fuel) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
    public float Fuel = fuel;
}

/// <summary>
/// Event raised on the user of a tool to see if they can actually use it.
/// </summary>
[ByRefEvent]
public struct ToolUserAttemptUseEvent(EntityUid? target)
{
    public EntityUid? Target = target;
    public bool Cancelled = false;
}
