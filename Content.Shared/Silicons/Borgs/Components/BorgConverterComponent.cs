using Content.Shared.DoAfter;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// Component for converting a borg to a randomly picked syndiborg on click, with a doafter.
/// The borg can be an empty chassis if you just want greentext but no friend.
/// Removed from the user after a borg is converted.
/// </summary>
[RegisterComponent, Access(typeof(SharedBorgConverterSystem))]
public sealed partial class BorgConverterComponent : Component
{
    /// <summary>
    /// List of chassis entities to choose from.
    /// Should only have a module fill, since brain + battery are carried over.
    /// </summary>
    [DataField]
    public List<EntProtoId> Syndiborgs = new()
    {
        "BorgChassisSyndicateAssaultNinja",
        "BorgChassisSyndicateMedicalNinja",
        "BorgChassisSyndicateSaboteurNinja"
    };

    /// <summary>
    /// Lawset to assign to the fresh syndiborg.
    /// </summary>
    [DataField]
    public ProtoId<SiliconLawsetPrototype> Lawset = "Ninja";

    /// <summary>
    /// Length of the doafter.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Sound played after converting a borg, everyone can hear it.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Popup shown to the borg when the doafter begins.
    /// </summary>
    [DataField]
    public LocId ConvertingPopup = "ninja-borg-converting";

    /// <summary>
    /// Popup shown to the borg when the doafter ends and they get converted.
    /// </summary>
    [DataField]
    public LocId ConvertedPopup = "ninja-borg-converted";
}

/// <summary>
/// Raised on the user after a borg is converted.
/// </summary>
[ByRefEvent]
public record struct BorgConvertedEvent(EntityUid Borg);

/// <summary>
/// Raised on the user when trying to convert a borg.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BorgConversionDoAfterEvent : SimpleDoAfterEvent
{
}
