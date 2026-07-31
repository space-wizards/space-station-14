// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.RoundEnd;

/// <summary>
/// Compact, entity-free description used to reconstruct a round-end manifest doll on the client.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class RoundEndDollData
{
    public EntProtoId? BodyPrototype;

    public ProtoId<StartingGearPrototype>? FallbackGear;

    public RoundEndHumanoidAppearance? Humanoid;

    public RoundEndDollEquipment[] Equipment = [];
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class RoundEndHumanoidAppearance
{
    public ProtoId<SpeciesPrototype> Species;

    public MarkingSet Markings = new();

    public HashSet<HumanoidVisualLayers> PermanentlyHidden = [];

    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    public Gender Gender;

    public int Age;

    public Color SkinColor;

    public Sex Sex;

    public Color EyeColor;

    public bool HairGradientEnabled;

    public Color HairGradientColor;
}

[Serializable, NetSerializable, DataDefinition]
public partial struct RoundEndDollEquipment
{
    public string Slot;

    public EntProtoId Prototype;
}
