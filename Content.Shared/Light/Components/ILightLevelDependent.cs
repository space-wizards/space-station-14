namespace Content.Shared.Light.Components;

/// <summary>
/// Light level dependent components need to implement this interface so the light level manager system will be able to set the next update time.
/// The update interval should reflect the importance of the behavior you're trying to implement.
/// </summary>
/// <remarks>
/// ex. A botany plant that needs to be illuminated to grow should not frequently recalculate its light level,
/// and can maybe have like a ~10 second update interval
/// ex2. A player with that is a shadow or darkness antag who requires light level to use their powers or take damage should update very frequently 
/// or every tick.
/// </remarks>
public interface ILightLevelDependent
{
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateCooldown { get; set; }
}