namespace Content.Server.Botany.Components;

/// <summary>
/// Anything that can be used to cross-pollinate plants.
/// </summary>
[RegisterComponent]
public sealed partial class BotanySwabComponent : Component
{
    /// <summary>
    /// Delay between swab uses.
    /// </summary>
    [DataField]
    public TimeSpan SwabDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// SeedData from the first plant that got swabbed.
    /// </summary>
    public SeedData? SeedData;
}
