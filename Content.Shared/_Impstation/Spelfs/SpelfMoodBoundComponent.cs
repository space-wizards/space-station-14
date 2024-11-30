using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Spelfs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSpelfMoodSystem))]
public sealed partial class SpelfMoodsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool FollowsSharedMoods = true;

    [DataField, ViewVariables, AutoNetworkedField]
    public List<SpelfMood> Moods = new();

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool CanBeEmagged = true;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public SoundSpecifier? MoodsChangedSound = new SoundPathSpecifier("/Audio/_Impstation/Spelf/moods_changed.ogg");

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
