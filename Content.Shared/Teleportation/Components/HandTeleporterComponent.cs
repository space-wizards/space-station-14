using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Creates portals. If two are created, both are linked together--otherwise the first teleports randomly.
///     Using it with both portals active deactivates both.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class HandTeleporterComponent : Component
{
    [ViewVariables]
    public EntityUid? FirstPortal = null;

    [ViewVariables]
    public EntityUid? SecondPortal = null;

    [DataField("firstPortalPrototype")]
    public string FirstPortalPrototype = "PortalRed";

    [DataField("secondPortalPrototype")]
    public string SecondPortalPrototype = "PortalBlue";

    [DataField("newPortalSound")]
    public SoundSpecifier NewPortalSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    [DataField("clearPortalsSound")]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
