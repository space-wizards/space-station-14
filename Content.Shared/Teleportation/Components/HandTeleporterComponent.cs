using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Creates portals. If two are created, both are linked together--otherwise the first teleports randomly.
///     Using it with both portals active deactivates both.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HandTeleporterComponent : Component
{
    [ViewVariables, DataField("firstPortal")]
    public EntityUid? FirstPortal = null;

    [ViewVariables, DataField("secondPortal")]
    public EntityUid? SecondPortal = null;

    [DataField("firstPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FirstPortalPrototype = "PortalRed";

    [DataField("secondPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SecondPortalPrototype = "PortalBlue";

    [DataField("newPortalSound")] public SoundSpecifier NewPortalSound =
        new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
        {
            Params = AudioParams.Default.WithVolume(-2f)
        };

    [DataField("clearPortalsSound")]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    ///     Delay for creating the portals in seconds.
    /// </summary>
    [DataField("portalCreationDelay")]
    public float PortalCreationDelay = 1.0f;
}

[Serializable, NetSerializable]
public sealed partial class TeleporterDoAfterEvent : SimpleDoAfterEvent
{
}
