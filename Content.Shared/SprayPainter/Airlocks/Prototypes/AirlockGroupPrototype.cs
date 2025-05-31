using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SprayPainter.Airlocks.Prototypes;

[Prototype]
public sealed partial class AirlockGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<string, ResPath> StylePaths = default!;

    // The priority determines, which sprite is used when showing
    // the icon for a style in the SprayPainter UI. The highest priority
    // gets shown.
    [DataField]
    public int IconPriority = 0;
}
