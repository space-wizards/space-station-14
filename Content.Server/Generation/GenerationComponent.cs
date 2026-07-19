namespace Content.Server.Generation;

/// <summary>
/// Makes it so that an entity is associated with a database-saved number which indicates how many shifts it has died in a row (1 for 0 death / first-generation to n for n-1 death / nth generation)
/// </summary>
[RegisterComponent]
public sealed partial class GenerationComponent : Component
{
    /// <summary>
    /// If this is true, then the entity will be displayed as "Pun Pun I". If false, the number wont be displayed for the first generation.
    /// </summary>
    [DataField]
    public bool ShowNumberOne = false;

    /// <summary>
    /// If true, then to count as having survived the entity needs to make it to CC.
    /// TODO!!!!
    /// </summary>
    [DataField]
    public bool MustEvac = true;

    /// <summary>
    /// Database key used to save this entity's data
    /// </summary>
    [DataField]
    public string DatabaseKey;

    /// <summary>
    /// Stores the generation number of this entity, retrieved from the DB.
    /// </summary>
    [Access(typeof(GenerationSystem)), ViewVariables]
    public uint GenerationNumber;
}
