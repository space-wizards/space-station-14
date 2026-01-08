using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Research.TechnologyDisk.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiskConsoleComponent : Component
{
    /// <summary>
    /// How much it costs to print a disk
    /// </summary>
    [DataField]
    public int PricePerDisk = 1000;

    /// <summary>
    /// The prototype of what's being printed
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DiskPrototype = "TechnologyDisk";

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
