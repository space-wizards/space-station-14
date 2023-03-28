using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Creates portals. If two are created, both are linked together--otherwise the first teleports randomly.
///     Using it with both portals active deactivates both.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class CluwneTeleporterComponent : Component
{
    [ViewVariables, DataField("firstPortal")]
    public EntityUid? FirstPortal = null;

    [ViewVariables, DataField("secondPortal")]
    public EntityUid? SecondPortal = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public float SpawnAmount = 1;

    [DataField("requiredMaterial", customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string RequiredMaterial = "Plasma";

    [DataField("materialPerAnomaly"), ViewVariables(VVAccess.ReadWrite)]
    public int MaterialPerAnomaly = 1500; // a bit less than a stack of plasma

    [DataField("firstPortalPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FirstPortalPrototype = "PortalGreeny";

    [DataField("secondPortalPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SecondPortalPrototype = "PortalYellow";

    [DataField("newPortalSound")]
    public SoundSpecifier NewPortalSound = new SoundPathSpecifier("/Audio/Magic/wand_teleport.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    [DataField("cooldownEndTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownEndTime = TimeSpan.Zero;

    /// <summary>
    /// The cooldown between generating anomalies.
    /// </summary>
    [DataField("cooldownLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownLength = TimeSpan.FromMinutes(1);

    [DataField("clearPortalsSound")]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Magic/wand_teleport.ogg");

    /// <summary>
    ///     Delay for creating the portals in seconds.
    /// </summary>
    [DataField("portalCreationDelay")]
    public float PortalCreationDelay = 4.5f;

}
