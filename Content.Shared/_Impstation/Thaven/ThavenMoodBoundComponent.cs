using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Thaven.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedThavenMoodSystem))]
public sealed partial class ThavenMoodsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool FollowsSharedMoods = true;

    [DataField, ViewVariables, AutoNetworkedField]
    public List<ThavenMood> Moods = new();

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool CanBeEmagged = true;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public SoundSpecifier? MoodsChangedSound = new SoundPathSpecifier("/Audio/_Impstation/Thaven/moods_changed.ogg");

    [DataField(serverOnly: true), ViewVariables]
    public EntityUid? Action;
}

public sealed partial class ToggleMoodsScreenEvent : InstantActionEvent
{
}

[NetSerializable, Serializable]
public enum ThavenMoodsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ThavenMoodsBuiState : BoundUserInterfaceState
{
    public List<ThavenMood> Moods;
    public List<ThavenMood> SharedMoods;

    public ThavenMoodsBuiState(List<ThavenMood> moods, List<ThavenMood> sharedMoods)
    {
        Moods = moods;
        SharedMoods = sharedMoods;
    }
}
