using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsoleComponent : Component
{
    /// <summary>
    /// Signal input to print disk.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> PrintPort = "PrintDisk";

    /// <summary>
    /// How much it costs to print a disk
    /// </summary>
    [DataField]
    public int PricePerDisk = 1000;

    /// <summary>
    /// The prototype of what's being printed
    /// </summary>
    [DataField]
    public EntProtoId DiskPrototype = "TechnologyDisk";

    /// <summary>
    /// How long it takes to print <see cref="DiskPrototype"/>
    /// </summary>
    [DataField]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
