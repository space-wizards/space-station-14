using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._Impstation.Thaven;

namespace Content.Client._Impstation.Thaven.Eui;

public sealed class ThavenMoodsEui : BaseEui
{
    private readonly EntityManager _entityManager;

    private ThavenMoodUi _thavenMoodUi;
    private NetEntity _target;

    public ThavenMoodsEui()
    {
        _entityManager = IoCManager.Resolve<EntityManager>();

        _thavenMoodUi = new ThavenMoodUi();
        _thavenMoodUi.SaveButton.OnPressed += _ => SaveMoods();
    }

    private void SaveMoods()
    {
        var newMoods = _thavenMoodUi.GetMoods();
        SendMessage(new ThavenMoodsSaveMessage(newMoods, _target));
        _thavenMoodUi.SetMoods(newMoods);
    }

    public override void Opened()
    {
        _thavenMoodUi.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ThavenMoodsEuiState s)
            return;

        _target = s.Target;
        _thavenMoodUi.SetMoods(s.Moods);
    }
}
