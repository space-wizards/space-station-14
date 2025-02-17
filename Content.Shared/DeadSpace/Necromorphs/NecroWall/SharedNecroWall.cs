// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Necromorphs.NecroWall;

[NetSerializable, Serializable]
public enum NecroWallVisuals : byte
{
    Stage1,
    Stage2,
    Stage3,
    Stage4
}
