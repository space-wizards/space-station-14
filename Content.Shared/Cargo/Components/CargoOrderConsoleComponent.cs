using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Handles sending order requests to cargo. Doesn't handle orders themselves via shuttle or telepads.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CargoOrderConsoleComponent : Component
{
    [DataField("soundError")] public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// All of the <see cref="CargoProductPrototype.Group"/>s that are supported.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("AllowedGroups", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> AllowedGroups = new();

    /// <summary>
    /// Radio channel on which order approval announcements are transmitted
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("AnnouncementChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string AnnouncementChannel = "Cargo";
}

