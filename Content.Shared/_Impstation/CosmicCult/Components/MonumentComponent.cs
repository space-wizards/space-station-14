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
    [NonSerialized] public const int LayerMask = 777;
    [DataField, AutoNetworkedField] public HashSet<ProtoId<InfluencePrototype>> UnlockedInfluences = [];
    [DataField, AutoNetworkedField] public HashSet<ProtoId<GlyphPrototype>> UnlockedGlyphs = [];
    [DataField, AutoNetworkedField] public ProtoId<GlyphPrototype> SelectedGlyph;
    [DataField, AutoNetworkedField] public int AvailableEntropy;
    [DataField, AutoNetworkedField] public int TotalEntropy;
    [DataField, AutoNetworkedField] public int EntropyUntilNextStage;
    [DataField, AutoNetworkedField] public int CrewToConvertNextStage;
    [DataField, AutoNetworkedField] public float PercentageComplete;
    /// <summary>
    /// A bool we use to set whether The Monument's UI is available or not.
    /// </summary>
    [DataField, AutoNetworkedField] public bool Enabled = true;
    /// <summary>
    /// A bool that determines whether The Monument is tangible to non-cultists.
    /// </summary>
    [DataField, AutoNetworkedField] public bool HasCollision = false;
    [DataField, AutoNetworkedField] public TimeSpan TransformTime = TimeSpan.FromSeconds(2.8);
    [DataField, AutoNetworkedField] public EntityUid? CurrentGlyph;
    [AutoPausedField, DataField] public TimeSpan VitalityCheckTimer = default!;
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
    Tier3,
}

[Serializable, NetSerializable]
public enum MonumentVisualLayers : byte
{
    MonumentLayer,
    TransformLayer,
    FinaleLayer,
}
