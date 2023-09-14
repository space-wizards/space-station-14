using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCuffableSystem))]
public sealed partial class HandcuffComponent : Component
{
    /// <summary>
    ///     The time it takes to cuff an entity.
    /// </summary>
    [DataField("cuffTime"), ViewVariables(VVAccess.ReadWrite)]
    public float CuffTime = 3.5f;

    /// <summary>
    ///     The time it takes to uncuff an entity.
    /// </summary>
    [DataField("uncuffTime"), ViewVariables(VVAccess.ReadWrite)]
    public float UncuffTime = 3.5f;

    /// <summary>
    ///     The time it takes for a cuffed entity to uncuff itself.
    /// </summary>
    [DataField("breakoutTime"), ViewVariables(VVAccess.ReadWrite)]
    public float BreakoutTime = 30f;

    /// <summary>
    ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
    /// </summary>
    [DataField("stunBonus"), ViewVariables(VVAccess.ReadWrite)]
    public float StunBonus = 2f;

    /// <summary>
    ///     Will the cuffs break when removed?
    /// </summary>
    [DataField("breakOnRemove"), ViewVariables(VVAccess.ReadWrite)]
    public bool BreakOnRemove;

    /// <summary>
    ///     Will the cuffs break when removed?
    /// </summary>
    [DataField("brokenPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string? BrokenPrototype;

    [DataField("damageOnResist"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamageOnResist = new()
    {
        DamageDict = new()
             {
                 { "Blunt", 3.0 },
             }
    };

    /// <summary>
    ///     The path of the RSI file used for the player cuffed overlay.
    /// </summary>
    [DataField("cuffedRSI"), ViewVariables(VVAccess.ReadWrite)]
    public string? CuffedRSI = "Objects/Misc/handcuffs.rsi";

    /// <summary>
    ///     The iconstate used with the RSI file for the player cuffed overlay.
    /// </summary>
    [DataField("bodyIconState"), ViewVariables(VVAccess.ReadWrite)]
    public string? OverlayIconState = "body-overlay";

    /// <summary>
    /// An opptional color specification for <see cref="OverlayIconState"/>
    /// </summary>
    [DataField("color"), ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.White;

    [DataField("startCuffSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg");

    [DataField("endCuffSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier EndCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg");

    [DataField("startBreakoutSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier StartBreakoutSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_breakout_start.ogg");

    [DataField("startUncuffSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier StartUncuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");

    [DataField("endUncuffSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier EndUncuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
}

[Serializable, NetSerializable]
public sealed class HandcuffComponentState : ComponentState
{
    public readonly string? IconState;

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
