using Content.Shared.DeviceLinking.Components;
using Content.Shared.UserInterface;

namespace Content.Shared.DeviceLinking.Systems;

public abstract class SharedRandomGateSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomGateComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
        SubscribeLocalEvent<RandomGateComponent, RandomGateProbabilityChangedMessage>(OnProbabilityChanged);
    }

    private void OnAfterActivatableUIOpen(EntityUid uid, RandomGateComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUI(uid, component);
    }

    private void OnProbabilityChanged(EntityUid uid, RandomGateComponent component, RandomGateProbabilityChangedMessage args)
    {
        component.SuccessProbability = Math.Clamp(args.Probability, 0f, 1f);
        UpdateUI(uid, component);
    }

    private void UpdateUI(EntityUid uid, RandomGateComponent component)
    {
        if (!_ui.HasUi(uid, RandomGateUiKey.Key))
            return;
        _ui.SetUiState(uid, RandomGateUiKey.Key, new RandomGateBoundUserInterfaceState(component.SuccessProbability));
    }
}
