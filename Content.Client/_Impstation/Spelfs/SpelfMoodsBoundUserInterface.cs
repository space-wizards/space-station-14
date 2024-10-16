using Content.Client._Impstation.Spelfs;
using Content.Shared._Impstation.Spelfs;
using Content.Shared._Impstation.Spelfs.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Spelfs;

[UsedImplicitly]
public sealed class SpelfMoodsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SpelfMoodsMenu? _menu;
    private EntityUid _owner;
    private List<SpelfMood>? _moods;
    private List<SpelfMood>? _sharedMoods;

    public SpelfMoodsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SpelfMoodsMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SpelfMoodsBuiState msg)
            return;

        _moods = msg.Moods;
        _sharedMoods = msg.SharedMoods;
        _menu?.Update(_owner, msg);
    }
}
