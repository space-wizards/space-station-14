using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsoleComponent : Component
{
    /// <summary>
    /// How much it costs to print a disk
    /// </summary>
    [DataField("pricePerDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePerDisk = 1000;

    /// <summary>
    /// The prototypes of what's being printed. Chosen randomly.
    /// </summary>
    [DataField("diskPrototypes", customTypeSerializer: typeof(PrototypeIdArraySerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string[] DiskPrototypes =
    [
        "TechnologyDiskIndustrialT1",
        "TechnologyDiskIndustrialT2",
        "TechnologyDiskIndustrialT3",
        "TechnologyDiskArsenalT1",
        "TechnologyDiskArsenalT2",
        "TechnologyDiskArsenalT3",
        "TechnologyDiskExperimentalT1",
        "TechnologyDiskExperimentalT2",
        "TechnologyDiskExperimentalT3",
        "TechnologyDiskCivilianServicesT1",
        "TechnologyDiskCivilianServicesT2",
        "TechnologyDiskCivilianServicesT3",
    ];

    /// <summary>
    /// How long it takes to print <see cref="DiskPrototype"/>
    /// </summary>
    [DataField("printDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
