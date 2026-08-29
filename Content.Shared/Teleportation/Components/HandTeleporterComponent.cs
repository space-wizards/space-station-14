using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Creates portals. If two are created, both are linked together--otherwise the first teleports randomly.
///     Using it with both portals active deactivates both.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HandTeleporterComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? FirstPortal;

    [DataField, AutoNetworkedField]
    public EntityUid? SecondPortal;

    /// <summary>
    ///     Should the portals be able to be placed across grids?
    /// </summary>
    [DataField]
    public bool AllowPortalsOnDifferentGrids;

    /// <summary>
    ///     Should the portals work across maps?
    /// </summary>
    [DataField]
    public bool AllowPortalsOnDifferentMaps;

    [DataField]
    public EntProtoId FirstPortalPrototype = "PortalRed";

    [DataField]
    public EntProtoId SecondPortalPrototype = "PortalBlue";

    [DataField]
    public SoundSpecifier NewPortalSound =
        new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
        {
            Params = AudioParams.Default.AddVolume(-2f)
        };

    [DataField]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    ///     Delay for creating the portals in seconds.
    /// </summary>
    [DataField]
    public float PortalCreationDelay = 1.0f;
}

[Serializable, NetSerializable]
public sealed partial class TeleporterDoAfterEvent : SimpleDoAfterEvent;
