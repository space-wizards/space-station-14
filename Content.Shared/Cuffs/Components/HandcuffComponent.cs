using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCuffableSystem))]
public sealed partial class HandcuffComponent : Component
{
    /// <summary>
    ///     The time it takes to cuff an entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CuffTime = 3.5f;

    /// <summary>
    ///     The time it takes to uncuff an entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float UncuffTime = 3.5f;

    /// <summary>
    ///     The time it takes for a cuffed entity to uncuff itself.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BreakoutTime = 15f;

    /// <summary>
    ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StunBonus = 2f;

    /// <summary>
    ///     Will the cuffs break when removed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool BreakOnRemove;

    /// <summary>
    ///     Will the cuffs break when removed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? BrokenPrototype;

    /// <summary>
    /// Whether the cuffs are currently being used to cuff someone.
    /// We need the extra information for when the virtual item is deleted because that can happen when you simply stop
    /// pulling them on the ground.
    /// </summary>
    [ViewVariables]
    public bool Used;

    /// <summary>
    ///     The path of the RSI file used for the player cuffed overlay.
    /// </summary>
    [DataField]
    public string? CuffedRSI = "Objects/Misc/handcuffs.rsi";

    /// <summary>
    ///     Valid RSI states that this specific handcuff supports.
    /// </summary>
    [DataField]
    public HashSet<string> ValidStates;

    /// <summary>
    /// An optional color specification for <see cref="CuffedRSI"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier EndCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier StartBreakoutSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_breakout_start.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier StartUncuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier EndUncuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
}

/// <summary>
/// Event fired on the User when the User attempts to uncuff the Target.
/// Should generate popups on the User.
/// </summary>
[ByRefEvent]
public record struct UncuffAttemptEvent(EntityUid User)
{
    public bool Cancelled = false;
}

/// <summary>
/// Event raised on an entity being uncuffed to determine any modifiers to the amount of time it takes to uncuff them.
/// </summary>
[ByRefEvent]
public record struct ModifyUncuffDurationEvent(EntityUid User, EntityUid Target, float Duration);
