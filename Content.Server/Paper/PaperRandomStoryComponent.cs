namespace Content.Server.Paper;

/// <summary>
///    Adds randomly generated stories to Paper component
/// </summary>
[RegisterComponent, Access(typeof(PaperRandomStorySystem))]
public sealed partial class PaperRandomStoryComponent : Component
{
    [DataField]
    public List<string>? StorySegments;

    [DataField]
    public string StorySeparator = " ";
}
