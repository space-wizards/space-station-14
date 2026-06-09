using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Implanters are used to implant or extract implants from an entity.
/// Some can be single use (implant only) or some can draw out an implant
/// </summary>
//TODO: Rework drawing to work with implant cases when surgery is in
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ImplanterComponent : Component
{
    public const string ImplanterSlotId = "implanter_slot";
    public const string ImplantSlotId = "implant";

    /// <summary>
    /// Whitelist to check entities against before implanting.
    /// Implants get their own whitelist which is checked afterwards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist to check entities against before implanting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Used for implanters that start with specific implants
    /// </summary>
    [DataField]
    public EntProtoId? Implant;

    /// <summary>
    /// The time it takes to implant someone else
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ImplantTime = 5f;

    //TODO: Remove when surgery is a thing
    /// <summary>
    /// The time it takes to extract an implant from someone
    /// It's excessively long to deter from implant checking any antag
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DrawTime = 25f;

    /// <summary>
    /// Good for single-use injectors
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ImplantOnly;

    /// <summary>
    /// The current mode of the implanter
    /// Mode is changed automatically depending if it implants or draws
    /// </summary>
    [DataField, AutoNetworkedField]
    public ImplanterToggleMode CurrentMode;

    /// <summary>
    /// The name and description of the implant to show on the implanter
    /// </summary>
    [DataField, AutoNetworkedField]
    public (string, string) ImplantData = ("", "");

    /// <summary>
    /// Determines if the same type of implant can be implanted into an entity multiple times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowMultipleImplants = false;

    /// <summary>
    /// The <see cref="ItemSlot"/> for this implanter
    /// </summary>
    [DataField(required: true)]
    public ItemSlot ImplanterSlot = new();

    /// <summary>
    /// If true, the implanter may be used to remove all kinds of (deimplantable) implants without selecting any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowDeimplantAll = false;

    /// <summary>
    /// Whitelist of implants that may be removed via this implanter
    /// </summary>
    [DataField]
    public EntityWhitelist DeimplantWhitelist = new();

    /// <summary>
    /// The list of implants that may be removed via this implanter
    /// </summary>
    [DataField]
    public List<EntProtoId> ImplantsList = new List<EntProtoId>();

    /// <summary>
    /// The subdermal implants that may be removed via this implanter
    /// </summary>
    [DataField]
    public DamageSpecifier DeimplantFailureDamage = new();

    /// <summary>
    /// Chosen implant to remove, if necessary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? DeimplantChosen = null;

    /// <summary>
    /// The sound to be played when an implanter catastrophically fails.
    /// </summary>
    [DataField]
    public SoundSpecifier ImplanterDrawFailSound  = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [ViewVariables]
    public bool UiUpdateNeeded;
}

/// <summary>
/// Indicates if the implanter is set to implant removal
/// or to implanting mode.
/// </summary>
[Serializable, NetSerializable]
public enum ImplanterToggleMode : byte
{
    Inject,
    Draw
}

[Serializable, NetSerializable]
public enum ImplanterVisuals : byte
{
    Full
}

[Serializable, NetSerializable]
public enum ImplanterImplantOnlyVisuals : byte
{
    ImplantOnly
}
