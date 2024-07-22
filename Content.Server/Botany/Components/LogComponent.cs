using Content.Server.Botany.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Components;
// TODO: This should probably be merged with SliceableFood somehow or made into a more generic Choppable.
// Yeah this is pretty trash. also consolidating this type of behavior will avoid future transform parenting bugs (see #6090).

[RegisterComponent]
[Access(typeof(LogSystem))]
public sealed partial class LogComponent : Component
{
    [DataField]
    public EntProtoId SpawnedPrototype = "MaterialWoodPlank1";

    [DataField("spawnCount")] public int SpawnCount = 2;
}
