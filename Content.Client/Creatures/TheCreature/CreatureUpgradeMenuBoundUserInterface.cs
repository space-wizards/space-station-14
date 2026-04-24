using Content.Client.Creatures.TheCreature.UI;
using Content.Shared.Creatures.TheCreature;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Creatures.TheCreature;

[UsedImplicitly]
public sealed class CreatureUpgradeMenuBoundUserInterface : BoundUserInterface
{
    private CreatureUpgradeMenuWindow? _window;

    public CreatureUpgradeMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CreatureUpgradeMenuWindow>();
        _window.OnEvolve += upgradeId => SendMessage(new CreatureEvolveMessage(upgradeId));

        if (EntMan.TryGetComponent<CreatureComponent>(Owner, out var comp))
        {
            _window.UpdateState(new CreatureUpgradeMenuBuiState(
                comp.BloodPool,
                comp.MaxBloodPool,
                comp.BloodConsumedTotal,
                new Dictionary<string, int>(comp.UpgradeRanks)));
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CreatureUpgradeMenuBuiState buiState)
            _window?.UpdateState(buiState);
    }
}
