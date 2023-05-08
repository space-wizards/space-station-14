using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Research.UI;

[UsedImplicitly]
public sealed class ResearchConsoleBoundUserInterface : BoundUserInterface
{
    private readonly IEntityManager _entityManager;

    public int Points { get; private set; }

    private ResearchConsoleMenu? _consoleMenu;
    private TechnologyDatabaseComponent? _technologyDatabase;

    private readonly SharedResearchSystem _research;

    public ResearchConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        SendMessage(new ConsoleServerSyncMessage());
        _entityManager = IoCManager.Resolve<IEntityManager>();
        _research = _entityManager.System<SharedResearchSystem>();
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner.Owner;

        if (!_entityManager.TryGetComponent(owner, out _technologyDatabase))
            return;

        _consoleMenu = new ResearchConsoleMenu(owner, this);

        _consoleMenu.OnClose += Close;

        _consoleMenu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (ResearchConsoleBoundInterfaceState)state;
        Points = castState.Points;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _consoleMenu?.Dispose();
    }
}
