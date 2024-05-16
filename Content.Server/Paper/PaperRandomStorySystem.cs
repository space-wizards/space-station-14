using Content.Shared.StoryGen;

namespace Content.Server.Paper;

public sealed class PaperRandomStorySystem : EntitySystem
{
    [Dependency] private readonly StoryGeneratorSystem _storyGen = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperRandomStoryComponent, MapInitEvent>(OnMapinit);
    }

    private void OnMapinit(Entity<PaperRandomStoryComponent> paperStory, ref MapInitEvent ev)
    {
        if (!TryComp<PaperComponent>(paperStory, out var paper))
            return;

        if (!_storyGen.TryGenerateStoryFromTemplate(paperStory.Comp.Template, out var story))
            return;

        paper.Content += $"\n{story}";
        Dirty(paperStory.Owner, paper);
    }
}
