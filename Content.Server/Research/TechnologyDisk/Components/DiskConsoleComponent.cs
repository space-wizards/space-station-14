using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed class DiskConsoleComponent : Component
{
    [DataField("pricePerDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePerDisk = 2500;

    [DataField("diskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DiskPrototype = "TechnologyDisk";

    [DataField("printDuration")]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
