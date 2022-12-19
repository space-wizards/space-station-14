using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed class DiskConsoleComponent : Component
{
    [DataField("pricePerDisk")]
    public int PricePerDisk = 3500;

    [DataField("diskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DiskPrototype = "TechnologyDisk";

    [DataField("printDuration")]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    [DataField("printCooldown")]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(10);

    public TimeSpan PrintFinish = TimeSpan.Zero;
}
