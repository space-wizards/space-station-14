using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._FTL.FTLPoints;

/// <summary>
/// The type of FTL point.
/// </summary>
///
[ImplicitDataDefinitionForInheritors, MeansImplicitUse]
public abstract class FTLPointSpawn
{
    public abstract void Effect(FTLPointSpawnArgs args);

    public readonly record struct FTLPointSpawnArgs(
        IEntityManager EntityManager,
        IMapManager MapManager
    );
}

