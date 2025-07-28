using Content.Server.EUI;
using Content.Shared.Starlight.NewLife;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI;

public sealed class NewLifeEui : BaseEui
{
    private readonly NewLifeSystem _newLifeSystem;
    private readonly HashSet<int> _usedSlots;
    private int _remainingLives;
    private int _maxLives;
    public NewLifeEui(HashSet<int> usedSlots, int remainingLives, int maxLives)
    {
        _newLifeSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<NewLifeSystem>();
        _usedSlots = usedSlots;
        _remainingLives = remainingLives;
        _maxLives = maxLives;
    }

    public override NewLifeEuiState GetNewState() => new()
    {
        UsedSlots = _usedSlots,
        RemainingLives = _remainingLives,
        MaxLives = _maxLives
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
