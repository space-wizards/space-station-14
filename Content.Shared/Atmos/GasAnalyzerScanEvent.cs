using Content.Server.Atmos;

namespace Content.Shared.Atmos;

/// <summary>
/// Raised when the analyzer is used. An atmospherics device that does not rely on a NodeContainer or
/// wishes to override the default analyzer behaviour of fetching all nodes in the attached NodeContainer
/// should subscribe to this and return the GasMixtures as desired. A device that is flippable should subscribe
/// to this event to report if it is flipped or not. See GasFilterSystem or GasMixerSystem for an example.
/// </summary>
public sealed class GasAnalyzerScanEvent : EntityEventArgs
{
    /// <summary>
    /// Key is the mix name (ex "pipe", "inlet", "filter"), value is the pipe direction and GasMixture. Add all mixes that should be reported when scanned.
    /// </summary>
    public Dictionary<string, GasMixture?>? GasMixtures;

    /// <summary>
    /// If the device is flipped. Flipped is defined as when the inline input is 90 degrees CW to the side input
    /// </summary>
    public bool DeviceFlipped;
}
