using Content.Server.RandomMetadata;

namespace Content.Server.Paper;

public sealed class PaperRandomStorySystem : EntitySystem
{

    [Dependency] private readonly RandomMetadataSystem _randomMeta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperRandomStoryComponent, MapInitEvent>(OnMapinit);
    }

    private void OnMapinit(Entity<PaperRandomStoryComponent> paperStory, ref MapInitEvent ev)
    {
        if (!TryComp<PaperComponent>(paperStory, out var paper))
            return;

        if (paperStory.Comp.StorySegments == null)
            return;

        var story = _randomMeta.GetRandomFromSegments(paperStory.Comp.StorySegments, paperStory.Comp.StorySeparator);

        paper.Content += $"\n{story}";
    }
}
