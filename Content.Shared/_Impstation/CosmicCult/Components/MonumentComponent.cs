using Content.Shared._Impstation.CosmicCult.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult.Components;
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class MonumentComponent : Component
{
    [NonSerialized] public static int LayerMask = 777;
    [DataField] public List<ProtoId<InfluencePrototype>> UnlockedInfluences = [];
    [DataField] public List<ProtoId<GlyphPrototype>> UnlockedGlyphs = [];
    [DataField] public ProtoId<GlyphPrototype> SelectedGlyph;
    [DataField] public int AvailableEntropy;
    [DataField] public int TotalEntropy;
    [DataField] public int EntropyUntilNextStage;
    [DataField] public int CrewToConvertNextStage;
    [DataField] public float PercentageComplete;
    /// <summary>
    /// A bool we use to set wether or not The Monument's UI is available or not.
    /// </summary>
    [DataField] public bool Enabled = true;
    /// <summary>
    /// A bool that determines wether or not the monument is tangible to non-cultists.
    /// </summary>
    [DataField] public bool HasCollision = false;
    [DataField, AutoNetworkedField] public TimeSpan TransformTime = TimeSpan.FromSeconds(2.8);
    [DataField, AutoNetworkedField] public EntityUid? CurrentGlyph;
    [AutoPausedField] public TimeSpan VitalityCheckTimer = default!;
    [DataField] public TimeSpan CheckWait = TimeSpan.FromSeconds(5);
    [DataField] public DamageSpecifier MonumentHealing = new()
    {
        DamageDict = new()
        {
            { "Blunt", 2},
            { "Slash", 2 },
            { "Piercing", 2 },
            { "Heat", 2},
            { "Shock", 2},
            { "Cold", 2},
            { "Poison", 2},
            { "Radiation", 2},
            { "Asphyxiation", 2 }
        }
    };

}
[Serializable, NetSerializable]
public sealed class InfluenceSelectedMessage(ProtoId<InfluencePrototype> influenceProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<InfluencePrototype> InfluenceProtoId = influenceProtoId;
}
[Serializable, NetSerializable]
public sealed class GlyphSelectedMessage(ProtoId<GlyphPrototype> glyphProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<GlyphPrototype> GlyphProtoId = glyphProtoId;
}

[Serializable, NetSerializable]
public sealed class GlyphRemovedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum MonumentVisuals : byte
{
    Monument,
    Transforming,
    FinaleReached,
    FinaleActive,
    Tier3
}

[Serializable, NetSerializable]
public enum MonumentVisualLayers : byte
{
    MonumentLayer,
    TransformLayer,
    FinaleLayer
}
