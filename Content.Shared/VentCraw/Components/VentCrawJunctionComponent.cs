// Initial file ported from the Starlight project repo, located at https://github.com/ss14Starlight/space-station-14

namespace Content.Shared.VentCraw.Components;

[RegisterComponent, Virtual]
public partial class VentCrawJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField("degrees")] public List<Angle> Degrees = new();
}
