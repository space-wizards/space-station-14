using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Maps;

/// <summary>
/// A MapModifier is a behaviour added to maps or stations in order to run modifications after loading.
/// Modifications can range from randomizing loot in vents, adding random mobs, procedurally generating
/// parts of the map etc.
/// </summary>
[DataDefinition, PublicAPI]
public abstract class MapModifier
{
    [DataField("priority", required: true)]
    public int Priority = 0;

    /// <summary>
    /// The modifier function that is executed. Override this in order to modify the station right after it loads.
    /// </summary>
    public virtual void Execute(MapId mapId, IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> grids) {}
}
