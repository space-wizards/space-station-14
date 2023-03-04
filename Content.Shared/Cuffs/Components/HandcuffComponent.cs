using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent]
public sealed class HandcuffComponent : Component
{
    /// <summary>
    ///     The time it takes to cuff an entity.
    /// </summary>
    [DataField("cuffTime")]
    public float CuffTime = 3.5f;

    /// <summary>
    ///     The time it takes to uncuff an entity.
    /// </summary>
    [DataField("uncuffTime")]
    public float UncuffTime = 3.5f;

    /// <summary>
    ///     The time it takes for a cuffed entity to uncuff itself.
    /// </summary>
    [DataField("breakoutTime")]
    public float BreakoutTime = 30f;

    /// <summary>
    ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
    /// </summary>
    [DataField("stunBonus")]
    public float StunBonus = 2f;

    /// <summary>
    ///     Will the cuffs break when removed?
    /// </summary>
    [DataField("breakOnRemove")]
    public bool BreakOnRemove;

    /// <summary>
    ///     Will the cuffs break when removed?
    /// </summary>
    [DataField("brokenPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? BrokenPrototype;

    /// <summary>
    ///     The path of the RSI file used for the player cuffed overlay.
    /// </summary>
    [DataField("cuffedRSI")]
    public string? CuffedRSI = "Objects/Misc/handcuffs.rsi";

    /// <summary>
    ///     The iconstate used with the RSI file for the player cuffed overlay.
    /// </summary>
    [DataField("bodyIconState")]
    public string? OverlayIconState = "body-overlay";

    [DataField("startCuffSound")]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg");

    [DataField("endCuffSound")]
    public SoundSpecifier EndCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg");

    [DataField("startBreakoutSound")]
    public SoundSpecifier StartBreakoutSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_breakout_start.ogg");

    [DataField("startUncuffSound")]
    public SoundSpecifier StartUncuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");

    [DataField("endUncuffSound")]
    public SoundSpecifier EndUncuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");

    [DataField("color")]
    public Color Color = Color.White;

    /// <summary>
    ///     Used to prevent DoAfter getting spammed.
    /// </summary>
    [DataField("cuffing")]
    public bool Cuffing;
}

[Serializable, NetSerializable]
public sealed class HandcuffComponentState : ComponentState
{
    public string? IconState { get; }

    public HandcuffComponentState(string? iconState)
    {
        IconState = iconState;
    }
}

/// <summary>
/// Event fired on the User when the User attempts to cuff the Target.
/// Should generate popups on the User.
/// </summary>
[ByRefEvent]
public record struct UncuffAttemptEvent(EntityUid User, EntityUid Target)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Target = Target;
    public bool Cancelled = false;
}
