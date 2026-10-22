using Content.Server._MACRO.Speech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server._MACRO.Speech.Components;

/// <summary>
/// Entities with this component require a certain piece of equipment in a certain slot in order to speak.
/// </summary>
[RegisterComponent]
[Access(typeof(SpeechRequiresEquipmentSystem))]
public sealed partial class SpeechRequiresEquipmentComponent : Component
{
    /// <summary>
    /// Whitelist for the equipment this entity requires to speak.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for the equipment this entity requires to speak.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Message played when you cannot speak.
    /// </summary>
    [DataField]
    public LocId? FailMessage;
}
