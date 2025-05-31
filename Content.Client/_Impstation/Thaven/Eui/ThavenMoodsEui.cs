using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._Impstation.Thaven;

namespace Content.Client._Impstation.Thaven.Eui;

public sealed class ThavenMoodsEui : BaseEui
{
    private ThavenMoodUi _thavenMoodUi;
    private NetEntity _target;

    public ThavenMoodsEui()
    {
        _thavenMoodUi = new ThavenMoodUi();
        _thavenMoodUi.OnSave += SaveMoods;
    }

    private void SaveMoods()
    {
        var newMoods = _thavenMoodUi.GetMoods();
        var toggle = _thavenMoodUi.ShouldFollowShared();
        SendMessage(new ThavenMoodsSaveMessage(newMoods, toggle, _target));
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
        _thavenMoodUi.SetFollowShared(s.FollowsShared);
        _thavenMoodUi.SetMoods(s.Moods);
    }
}
