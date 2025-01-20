using Content.Shared.Paper;
using Content.Shared.StoryGen;

namespace Content.Server.Paper;

public sealed class PaperRandomStorySystem : EntitySystem
{
    [Dependency] private readonly StoryGeneratorSystem _storyGen = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperRandomStoryComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PaperRandomStoryComponent> paperStory, ref MapInitEvent ev)
    {
        if (!TryComp<PaperComponent>(paperStory, out var paper))
            return;

        if (!_storyGen.TryGenerateStoryFromTemplate(paperStory.Comp.Template, out var story))
            return;

        _paper.SetContent((paperStory.Owner, paper), story);
    }
}
