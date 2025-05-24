namespace Content.Shared.Procedural.PostGeneration;

// Ime a worm
/// <summary>
/// Generates worm corridors.
/// </summary>
public sealed partial class WormCorridorDunGen : IDunGenLayer
{
    [DataField]
    public int PathLimit = 2048;

    /// <summary>
    /// How many times to run the worm
    /// </summary>
    [DataField]
    public int Count = 20;

    /// <summary>
    /// How long to make each worm
    /// </summary>
    [DataField]
    public int Length = 20;

    /// <summary>
    /// Maximum amount the angle can change in a single step.
    /// </summary>
    [DataField]
    public Angle MaxAngleChange = Angle.FromDegrees(45);

    /// <summary>
    /// How wide to make the corridor.
    /// </summary>
    [DataField]
    public float Width = 3f;
}
