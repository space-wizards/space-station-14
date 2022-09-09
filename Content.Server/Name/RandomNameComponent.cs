namespace Content.Server.Name;

/// <summary>
/// This is used for generating a random name for an entity
/// on Init based on a list of words and dataset prototypes.
/// </summary>
[RegisterComponent]
public sealed class RandomNameComponent : Component
{
    /// <summary>
    /// List of segments that will be joined with spaces to create the name.
    /// </summary>
    /// <remarks>
    /// This doesn't have a type serializer because it works fine with non-dataset prototypes.
    /// In fact, that's useful for things like "The [random name here]".
    /// </remarks>
    [DataField("segments")]
    public List<string>? Segments;
}
