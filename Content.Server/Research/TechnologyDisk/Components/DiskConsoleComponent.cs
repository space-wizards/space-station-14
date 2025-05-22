using Content.Shared.Random;
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
    /// A prototype for the tech disk entity.
    /// </summary>
    [DataField]
    public EntProtoId DiskPrototype = "TechnologyDisk";

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
