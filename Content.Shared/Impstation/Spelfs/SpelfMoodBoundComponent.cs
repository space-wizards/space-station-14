using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Impstation.Spelfs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpelfMoodsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool FollowsSharedMoods = true;

    [DataField, ViewVariables, AutoNetworkedField]
    public List<SpelfMood> Moods = new();

    [DataField(serverOnly: true), ViewVariables]
    public EntityUid? Action;
}

public sealed partial class ToggleMoodsScreenEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SpelfMoodsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SpelfMoodsBuiState : BoundUserInterfaceState
{
    public List<SpelfMood> Moods;
    public List<SpelfMood> SharedMoods;

    public SpelfMoodsBuiState(List<SpelfMood> moods, List<SpelfMood> sharedMoods)
    {
        Moods = moods;
        SharedMoods = sharedMoods;
    }
}
