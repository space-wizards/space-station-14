using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Teleportation.Components;

/// <summary>
///     Creates portals. If two are created, both are linked together--otherwise the first teleports randomly.
///     Using it with both portals active deactivates both.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class DimensionPotComponent : Component
{
    [ViewVariables, DataField("potPortal")]
    public EntityUid? PotPortal = null;

    [ViewVariables, DataField("dimensionPortal")]
    public EntityUid? DimensionPortal = null;
	
	[ViewVariables, DataField("portalsActive")]
	public bool PortalsActive = false;
	
	[DataField("pocketDimensionMap")];
	public MapId PocketDimensionMap = MapId.Nullspace;

    [DataField("potPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PotPortalPrototype = "PortalRed";

    [DataField("dimensionPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DimensionPortalPrototype = "PortalBlue";

    [DataField("openPortalsSound")] public SoundSpecifier OpenPortalsSound =
        new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
        {
            Params = AudioParams.Default.WithVolume(-2f)
        };

    [DataField("clearPortalsSound")]
    public SoundSpecifier ClearPortalsSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
