namespace Content.Server.Paper;

/// <summary>
///     Randomizes the book story by creating it from list of dataset prototypes or strings.
/// </summary>
[RegisterComponent, Access(typeof(PaperRandomStorySystem))]
public sealed partial class PaperRandomStoryComponent : Component
{
    [DataField]
    public List<string>? StorySegments;

    [DataField]
    public string StorySeparator = " ";
}
