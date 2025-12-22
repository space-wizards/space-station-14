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

    private void OnAfterActivatableUIOpen(Entity<RandomGateComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    private void OnProbabilityChanged(Entity<RandomGateComponent> ent, ref RandomGateProbabilityChangedMessage args)
    {
        ent.Comp.SuccessProbability = Math.Clamp(args.Probability, 0f, 100f) / 100f;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void UpdateUI(Entity<RandomGateComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, RandomGateUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, RandomGateUiKey.Key, new RandomGateBoundUserInterfaceState(ent.Comp.SuccessProbability));
    }
}
