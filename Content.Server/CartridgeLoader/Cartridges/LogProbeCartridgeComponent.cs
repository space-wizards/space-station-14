using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Audio;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class LogProbeCartridgeComponent : Component
{
    /// <summary>
    /// The list of pulled access logs
    /// </summary>
    [DataField("pulledAccessLog")]
    public List<PulledAccessLog> PulledAccessLogs = new();

    [DataField("soundScan")]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}


