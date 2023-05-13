namespace Content.Client.Storage.Visualizers;

[RegisterComponent]
[Access(typeof(EntityStorageVisualizerSystem))]
public sealed class EntityStorageVisualsComponent : Component
{
    /// <summary>
    /// 
    /// </summary>
    [DataField("state")]
    public string? StateBase;
    
    /// <summary>
    /// 
    /// </summary>
    [DataField("stateAlt")]
    public string? StateBaseAlt;
    
    /// <summary>
    /// 
    /// </summary>
    [DataField("stateOpen")]
    public string? StateOpen;
    
    /// <summary>
    /// 
    /// </summary>
    [DataField("stateClosed")]
    public string? StateClosed;
}
