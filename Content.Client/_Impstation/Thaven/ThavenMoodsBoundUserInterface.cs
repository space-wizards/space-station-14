using Content.Client._Impstation.Thaven;
using Content.Shared._Impstation.Thaven;
using Content.Shared._Impstation.Thaven.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Thaven;

[UsedImplicitly]
public sealed class ThavenMoodsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ThavenMoodsMenu? _menu;
    private EntityUid _owner;
    private List<ThavenMood>? _moods;
    private List<ThavenMood>? _sharedMoods;

    public ThavenMoodsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ThavenMoodsMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ThavenMoodsBuiState msg)
            return;

        _moods = msg.Moods;
        _sharedMoods = msg.SharedMoods;
        _menu?.Update(_owner, msg);
    }
}
