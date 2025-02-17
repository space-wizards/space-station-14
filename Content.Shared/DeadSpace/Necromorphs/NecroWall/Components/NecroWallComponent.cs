// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Necromorphs.NecroWall.Components;

[RegisterComponent]
public sealed partial class NecroWallComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? WallEntity = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTick = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickUtilRegen = TimeSpan.Zero;

    [DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadOnly)]
    public string NecroWallId = "NecroWall";

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool WallIsCaptured = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float LvlStage = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Regen = 10f;

    #region Visualizer

    [DataField("stage1")]
    public string Stage1 = "stage1";

    [DataField("stage2")]
    public string Stage2 = "stage2";

    [DataField("stage3")]
    public string Stage3 = "stage3";

    [DataField("stage4")]
    public string Stage4 = "stage4";

    #endregion
}

[ByRefEvent]
public readonly record struct CaptureWallEvent();
