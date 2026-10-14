using System.Linq;
using Content.Shared.Genetics.GeneticsConsole;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Genetics.UI;

[UsedImplicitly]
public sealed class GeneticsConsoleBoundUserInterface : BoundUserInterface
{
    private GeneticsConsoleWindow? _window;
    private GeneticsConsoleBoundUserInterfaceState? _lastState;

    public GeneticsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _lastState = null;
        _window = this.CreateWindow<GeneticsConsoleWindow>();
        _window.Title = Loc.GetString("genetics-console-window-title");
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var geneticsState = (GeneticsConsoleBoundUserInterfaceState) state;
        if (_window == null || _lastState != null && StatesEqual(_lastState, geneticsState))
            return;

        _lastState = geneticsState;
        _window.Populate(
            geneticsState,
            OnMutationAction,
            OnCreateInjector,
            () => SendMessage(new GeneticsConsoleMessage(GeneticsConsoleAction.HumanizeMonkey, string.Empty)),
            () => SendMessage(new GeneticsConsoleMessage(GeneticsConsoleAction.ScrambleDna, string.Empty)));
    }

    private void OnMutationAction(
        GeneticsMutationUiEntry mutation,
        GeneticsConsoleAction action,
        string? sequence = null,
        string? complementSequence = null)
    {
        SendMessage(new GeneticsConsoleMessage(
            action,
            mutation.Id,
            sequence ?? string.Empty,
            complementSequence ?? string.Empty));
    }

    private void OnCreateInjector(string mutationId)
    {
        SendMessage(new GeneticsConsoleMessage(GeneticsConsoleAction.CreateInjector, mutationId));
    }

    private static bool StatesEqual(
        GeneticsConsoleBoundUserInterfaceState left,
        GeneticsConsoleBoundUserInterfaceState right)
    {
        return left.ScannerConnected == right.ScannerConnected &&
               left.ScannerInRange == right.ScannerInRange &&
               left.SubjectName == right.SubjectName &&
               left.Instability == right.Instability &&
               left.Health == right.Health &&
               left.MaxHealth == right.MaxHealth &&
               left.GeneticDamage == right.GeneticDamage &&
               left.MaxGeneticDamage == right.MaxGeneticDamage &&
               left.CanHumanizeSubject == right.CanHumanizeSubject &&
               left.IsHumanoidSubject == right.IsHumanoidSubject &&
               left.ResultMessage == right.ResultMessage &&
               left.MaxMemorySlots == right.MaxMemorySlots &&
               left.JokerCooldown == right.JokerCooldown &&
               left.Mutations.Count == right.Mutations.Count &&
               left.Memory.Count == right.Memory.Count &&
               left.Mutations.Zip(right.Mutations).All(pair =>
                   pair.First.Id == pair.Second.Id &&
                   pair.First.Name == pair.Second.Name &&
                   pair.First.Description == pair.Second.Description &&
                   pair.First.Sequence == pair.Second.Sequence &&
                   pair.First.CandidateSequence == pair.Second.CandidateSequence &&
                   pair.First.CandidateComplementSequence == pair.Second.CandidateComplementSequence &&
                   pair.First.SequenceProgress == pair.Second.SequenceProgress &&
                   pair.First.Discovered == pair.Second.Discovered &&
                   pair.First.Active == pair.Second.Active &&
                   pair.First.Instability == pair.Second.Instability &&
                   pair.First.KnownPositions.Count == pair.Second.KnownPositions.Count &&
                   pair.First.KnownPositions.All(pair.Second.KnownPositions.Contains) &&
                   pair.First.HalfKnownPositions.Count == pair.Second.HalfKnownPositions.Count &&
                   pair.First.HalfKnownPositions.All(pair.Second.HalfKnownPositions.Contains)) &&
               left.Memory.Zip(right.Memory).All(pair =>
                   pair.First.Id == pair.Second.Id &&
                   pair.First.Name == pair.Second.Name &&
                   pair.First.Description == pair.Second.Description &&
                   pair.First.Instability == pair.Second.Instability);
    }
}
