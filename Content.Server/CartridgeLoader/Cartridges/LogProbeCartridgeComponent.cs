using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Audio;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
[Access(typeof(LogProbeCartridgeSystem))]
public sealed partial class LogProbeCartridgeComponent : Component
{
    /// <summary>
    /// The list of pulled access logs
    /// </summary>
    [DataField("pulledAccessLog")] [ViewVariables(VVAccess.ReadOnly)]
    public List<PulledAccessLog> PulledAccessLogs = new();

    [DataField("soundScan")] [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}


