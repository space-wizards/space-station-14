using Content.Server.Atmos;
using Content.Shared.Mech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Mech;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedMechComponent))]
public sealed class MechComponent : SharedMechComponent
{
    [DataField("airtight"), ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight = false;

    [DataField("startingEquipment", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingEquipment = new();

    //TODO: this doesn't support a tank implant for
    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air = new (GasMixVolume);
    public const float GasMixVolume = 70f;
}
