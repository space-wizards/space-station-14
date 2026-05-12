using Content.Client.Body.Systems;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;
using Content.Shared.Traits.Assorted;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.HealthAnalyzer.UI;

[UsedImplicitly]
public sealed partial class HealthAnalyzerBoundUserInterface : BoundUserInterface
{
    [Dependency] private IGameTiming _timing = default!;
    private readonly BloodstreamSystem _bloodstream = default!;
    private readonly SolutionContainerSystem _solutionContainer = default!;

    [ViewVariables]
    private HealthAnalyzerWindow? _window;

    public HealthAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _bloodstream = EntMan.System<BloodstreamSystem>();
        _solutionContainer = EntMan.System<SolutionContainerSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HealthAnalyzerWindow>();

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not HealthAnalyzerUiState cast)
            return;

        _window.Populate(cast);
    }

    /// <summary>
    /// This will update the UI to reflect the newest health changes of the scanned entity.
    /// This gets called in the <see cref="SharedHealthAnalyzerSystem"/>.
    /// </summary>
    public override void Update()
    {
        if (_window == null || _timing.ApplyingState || !EntMan.TryGetComponent<HealthAnalyzerComponent>(Owner, out var analyzer))
            return;

        if (analyzer.ScannedEntity is null)
        {
            _window.Close();
            return;
        }

        var netEntity = EntMan.GetNetEntity(analyzer.ScannedEntity);

        var entity = analyzer.ScannedEntity.Value;

        var bloodlevel = float.NaN;
        var bleeding = false;
        var unrevivable = false;

        if (EntMan.TryGetComponent<BloodstreamComponent>(entity, out var bloodstream) &&
            _solutionContainer.ResolveSolution(entity, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out _))
        {
            bloodlevel = _bloodstream.GetBloodLevel(entity);
            bleeding = bloodstream.BleedAmount > 0;
        }

        if (EntMan.TryGetComponent<UnrevivableComponent>(entity, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        _window.Populate(netEntity, analyzer.IsAnalyzerActive, bloodlevel, unrevivable, bleeding);
    }
}

