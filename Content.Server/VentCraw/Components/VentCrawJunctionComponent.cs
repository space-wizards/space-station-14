namespace Content.Server.VentCraw.Components;

[RegisterComponent]
[Access(typeof(VentCrawTubeSystem))]
[Virtual]
public class VentCrawJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField("degrees")] public List<Angle> Degrees = new();
}
