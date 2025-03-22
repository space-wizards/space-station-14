using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Tabletop.Game;

/// <summary>
/// A <see cref="TabletopSetup"/> which spawns <see cref="BoardPrototype"/> in the center of the tabletop space and
/// nothing else.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopEmptySetup : TabletopSetup
{
    /// <summary>
    /// The board to spawn.
    /// </summary>
    [DataField]
    public EntProtoId BoardPrototype = default!;

    protected override void SetupTabletop(Spawner spawner)
    {
        spawner.Spawn(BoardPrototype, 0, 0);
    }
}
