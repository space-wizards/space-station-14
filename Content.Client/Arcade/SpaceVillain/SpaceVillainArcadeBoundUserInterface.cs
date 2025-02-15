using Content.Shared.Arcade.SpaceVillain.Events;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.SpaceVillain;

public sealed class SpaceVillainArcadeBoundUserInterface : BoundUserInterface
{
    private SpaceVillainArcadeMenu? _menu;

    public SpaceVillainArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SpaceVillainArcadeMenu>();

        _menu.OnAttack += () => SendMessage(new SpaceVillainAttackActionMessage());
        _menu.OnHeal += () => SendMessage(new SpaceVillainHealActionMessage());
        _menu.OnRecharge += () => SendMessage(new SpaceVillainRechargeActionMessage());
        _menu.OnNewGame += () => SendMessage(new SpaceVillainNewGameActionMessage());
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case SpaceVillainInitialDataMessage initial:
                _menu!.VillainNameLabel.Text = initial.VillainName;
                _menu?.UpdateData(initial.PlayerHP, initial.PlayerMP, initial.VillainHP, initial.VillainMP);
                break;
            case SpaceVillainUpdateDataMessage update:
                _menu?.UpdateData(update.PlayerHP, update.PlayerMP, update.VillainHP, update.VillainMP);
                break;
        }
    }
}
