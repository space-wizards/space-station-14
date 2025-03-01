using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicMalignRiftComponent : Component
{
    [DataField] public bool Used = false;
    [DataField] public bool Occupied = false;
    [DataField] public EntProtoId PurgeVFX = "CleanseEffectVFX";
    [DataField] public SoundSpecifier PurgeSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/cleanse_deconversion.ogg");
    [DataField] public EntProtoId GrailID = "NullRodGrail";
    [DataField] public TimeSpan BibleTime = TimeSpan.FromSeconds(35);
    [DataField] public TimeSpan ChaplainTime = TimeSpan.FromSeconds(20);
    [DataField] public TimeSpan AbsorbTime = TimeSpan.FromSeconds(35);
}
