using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._Impstation.Spelfs;

namespace Content.Client._Impstation.Spelfs.Eui;

public sealed class SpelfMoodsEui : BaseEui
{
    private readonly EntityManager _entityManager;

    private SpelfMoodUi _spelfMoodUi;
    private NetEntity _target;

    public SpelfMoodsEui()
    {
        _entityManager = IoCManager.Resolve<EntityManager>();

        _spelfMoodUi = new SpelfMoodUi();
        _spelfMoodUi.SaveButton.OnPressed += _ => SaveMoods();
    }

    private void SaveMoods()
    {
        var newMoods = _spelfMoodUi.GetMoods();
        SendMessage(new SpelfMoodsSaveMessage(newMoods, _target));
        _spelfMoodUi.SetMoods(newMoods);
    }

    public override void Opened()
    {
        _spelfMoodUi.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not SpelfMoodsEuiState s)
            return;

        _target = s.Target;
        _spelfMoodUi.SetMoods(s.Moods);
    }
}
