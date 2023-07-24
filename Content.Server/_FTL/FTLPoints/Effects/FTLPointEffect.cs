using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._FTL.FTLPoints.Effects;

/// <summary>
/// The type of FTL point.
/// </summary>
///
[ImplicitDataDefinitionForInheritors, MeansImplicitUse]
public abstract class FTLPointEffect
{
    [DataField("probability")] public float Probability = 1f;
    public abstract void Effect(FTLPointEffectArgs args);

    public readonly record struct FTLPointEffectArgs(
        EntityUid MapUid,
        MapId MapId,
        IEntityManager EntityManager,
        IMapManager MapManager
    );
}
