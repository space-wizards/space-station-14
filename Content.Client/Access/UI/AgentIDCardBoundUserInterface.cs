using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.StatusIcon;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Access.UI;

/// <summary>
/// Initializes a <see cref="AgentIDCardWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AgentIDCardWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AgentIDCardWindow>();

        _window.OnNameChanged += OnNameChanged;
        _window.OnJobChanged += OnJobChanged;
        _window.OnJobIconChanged += OnJobIconChanged;

        Update();
    }

    private void OnNameChanged(string newName)
    {
        SendPredictedMessage(new AgentIDCardNameChangedMessage(newName));
    }

    private void OnJobChanged(string newJob)
    {
        SendPredictedMessage(new AgentIDCardJobChangedMessage(newJob));
    }

    public void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
    {
        SendPredictedMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
    }

    public override void Update()
    {
        if (!EntMan.TryGetComponent<IdCardComponent>(Owner, out var idCard))
            return;

        _window?.SetCurrentName(idCard.FullName ?? "");
        _window?.SetCurrentJob(idCard.LocalizedJobTitle ?? "");
        _window?.SetAllowedIcons(idCard.JobIcon);
    }
}
