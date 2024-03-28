using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cargo.Components;

[RegisterComponent]
public sealed partial class CargoBountyConsoleComponent : Component
{
    /// <summary>
    /// The id of the label entity spawned by the print label button.
    /// </summary>
    [DataField("bountyLabelId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BountyLabelId = "PaperCargoBountyManifest";

    /// <summary>
    /// The time at which the console will be able to print a label again.
    /// </summary>
    [DataField("nextPrintTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPrintTime = TimeSpan.Zero;

    /// <summary>
    /// The time between prints.
    /// </summary>
    [DataField("printDelay")]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// The time at which the console will be able to skip the next bounty.
    /// </summary>
    [DataField("nextSkipTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSkipTime = TimeSpan.Zero;

    /// <summary>
    /// The time between skipping bounties.
    /// </summary>
    [DataField("skipDelay")]
    public TimeSpan SkipDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The sound made when the bounty is skipped.
    /// </summary>
    [DataField("skipSound")]
    public SoundSpecifier SkipSound = new SoundPathSpecifier("/Audio/Effects/cargo.ogg");
}

[NetSerializable, Serializable]
public sealed class CargoBountyConsoleState : BoundUserInterfaceState
{
    public List<CargoBountyData> Bounties;

    public CargoBountyConsoleState(List<CargoBountyData> bounties)
    {
        Bounties = bounties;
    }
}

[Serializable, NetSerializable]
public sealed class BountyPrintLabelMessage : BoundUserInterfaceMessage
{
    public string BountyId;

    public BountyPrintLabelMessage(string bountyId)
    {
        BountyId = bountyId;
    }
}

[Serializable, NetSerializable]
public sealed class BountySkipMessage : BoundUserInterfaceMessage
{
    public string BountyId;
    public EntityUid SkippingEntityUid;

    public BountySkipMessage(string bountyId, EntityUid skippingEntityUid)
    {
        BountyId = bountyId;
        SkippingEntityUid = skippingEntityUid;
    }
}
