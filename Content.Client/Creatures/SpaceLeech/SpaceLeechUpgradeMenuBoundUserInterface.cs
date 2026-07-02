using Content.Client.Creatures.SpaceLeech.UI;
using Content.Shared.Creatures.SpaceLeech;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Creatures.SpaceLeech;

[UsedImplicitly]
public sealed class SpaceLeechUpgradeMenuBoundUserInterface : BoundUserInterface
{
    private SpaceLeechUpgradeMenuWindow? _window;

    public SpaceLeechUpgradeMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpaceLeechUpgradeMenuWindow>();
        _window.OnEvolve += upgradeId => SendMessage(new SpaceLeechEvolveMessage(upgradeId));

        if (EntMan.TryGetComponent<SpaceLeechComponent>(Owner, out var comp))
        {
            _window.UpdateState(new SpaceLeechUpgradeMenuBuiState(
                comp.BloodPool,
                comp.MaxBloodPool,
                comp.BloodConsumedTotal,
                new Dictionary<string, int>(comp.UpgradeRanks)));
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SpaceLeechUpgradeMenuBuiState buiState)
            _window?.UpdateState(buiState);
    }
}
