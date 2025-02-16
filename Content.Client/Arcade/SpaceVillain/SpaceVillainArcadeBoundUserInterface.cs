using Content.Shared.Arcade.SpaceVillain.Events;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.SpaceVillain;

public sealed class SpaceVillainArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SpaceVillainArcadeMenu? _menu;

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
        if (message is SpaceVillainUpdateDataMessage update)
            _menu?.UpdateData(update.PlayerHP, update.PlayerMP, update.VillainName, update.VillainHP, update.VillainMP, update.PlayerStatus, update.VillainStatus);
    }
}
