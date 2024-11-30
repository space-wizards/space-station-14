using Content.Shared._Impstation.CrewMedal;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.CrewMedal.UI;

/// <summary>
/// Initializes a <see cref="CrewMedalWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class CrewMedalBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [ViewVariables]
    private CrewMedalWindow? _window;

    public CrewMedalBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CrewMedalWindow>();

        _window.OnReasonChanged += OnReasonChanged;
        Reload();
    }

    private void OnReasonChanged(string newReason)
    {
        if (_entManager.TryGetComponent<CrewMedalComponent>(Owner, out var component) &&
            component.Reason.Equals(newReason))
            return;

        SendPredictedMessage(new CrewMedalReasonChangedMessage(newReason));
    }

    public void Reload()
    {
        if (_window == null || !_entManager.TryGetComponent<CrewMedalComponent>(Owner, out var component))
            return;

        _window.SetCurrentReason(component.Reason);
        _window.SetAwarded(component.Awarded);
        _window.SetMaxCharacters(component.MaxCharacters);
    }
}
