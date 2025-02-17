using Content.Client.Research.UI;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Research;

public sealed class ResearchSystem : SharedResearchSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchServerComponent, AfterAutoHandleStateEvent>(OnAnalyzerAfterAutoHandleState);
    }

    private void OnAnalyzerAfterAutoHandleState(Entity<ResearchServerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<ResearchConsoleBoundUserInterface>(ent.Owner, ResearchConsoleUiKey.Key, out var bui))
            bui.Update(ent);
    }
}
