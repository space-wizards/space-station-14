namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GameDirectorSystem))]
public sealed partial class GameStoriesComponent : Component
{
    /// <summary>
    ///   All possible story beats, by ID. These will be copied into GameDirectorSystemComponent
    ///
    ///   If we specify the same story beat as GameDirectorSystemComponent, the beat here wins (and overwrites).
    /// </summary>
    [DataField("storyBeats"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, StoryBeat> StoryBeats = new();

    /// <summary>
    ///   A dictionary mapping story names to the list of beats for each story.
    ///   One of these get picked randomly each time the current story is exausted.
    ///
    ///   These will be copied into GameDirectorSystemComponent
    /// </summary>
    [DataField("stories"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, Story> Stories = new();

    /// <summary>
    ///   Do we add our stories to the base class, or do we overwrite them wholesale?
    /// </summary>
    [DataField("overwriteStories"), ViewVariables(VVAccess.ReadWrite)]
    public bool OverwriteStories = false;
}
