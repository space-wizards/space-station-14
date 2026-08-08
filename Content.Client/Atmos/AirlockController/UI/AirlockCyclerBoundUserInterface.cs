using Content.Shared.Atmos.AirlockController;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.AirlockController.UI;

public sealed class AirlockCyclerBoundUserInterface : BoundUserInterface
{
    private AirlockCyclerWindow? _window;

    public AirlockCyclerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AirlockCyclerWindow>();
        _window.SetEntity(Owner);

        _window.CycleRequested += () => SendMessage(new AirlockCyclerCycleMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AirlockCyclerUiState cast)
            _window?.UpdateState(cast);
    }
}
