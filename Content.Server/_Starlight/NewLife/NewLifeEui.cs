using Content.Server.EUI;
using Content.Shared._Starlight.NewLife;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI;

public sealed class NewLifeEui : BaseEui
{
    private readonly NewLifeSystem _newLifeSystem;
    private readonly HashSet<int> _usedSlots;
    public NewLifeEui(HashSet<int> usedSlots)
    {
        _newLifeSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<NewLifeSystem>();
        _usedSlots = usedSlots;
    }

    public override NewLifeEuiState GetNewState() => new()
    {
        UsedSlots = _usedSlots
    };

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
    }

    public override void Closed()
    {
        base.Closed();

        _newLifeSystem.CloseEui(Player);
    }
}
