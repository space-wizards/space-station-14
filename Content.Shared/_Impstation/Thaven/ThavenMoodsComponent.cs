using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Thaven.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedThavenMoodSystem))]
public sealed partial class ThavenMoodsComponent : Component
{
    /// <summary>
    /// Whether to include SharedMoods that all thaven have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FollowsSharedMoods = true;

    /// <summary>
    /// The non-shared moods that are active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ThavenMood> Moods = new();

    /// <summary>
    /// Whether to allow emagging to add a random wildcard mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanBeEmagged = true;

    /// <summary>
    /// Whether to allow ion storms to add a random mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IonStormable = true;

    /// <summary>
    /// The probability that an ion storm will remove a mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IonStormRemoveChance = 0.25f;

    /// <summary>
    /// The probability that an ion storm will add a mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IonStormAddChance = 0.25f;

    /// <summary>
    /// The probability that an ion storm will pull a mood from the wildcard dataset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IonStormWildcardChance = 0.2f;

    /// <summary>
    /// The maximum number of moods that en entity can be given by ion storms.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxIonMoods = 4;

    /// <summary>
    /// Notification sound played if your moods change.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? MoodsChangedSound = new SoundPathSpecifier("/Audio/_Impstation/Thaven/moods_changed.ogg");

    [DataField(serverOnly: true)]
    public EntityUid? Action;
}

public sealed partial class ToggleMoodsScreenEvent : InstantActionEvent;

[NetSerializable, Serializable]
public enum ThavenMoodsUiKey : byte
{
    Key
}

/// <summary>
/// BUI state to tell the client what the shared moods are.
/// </summary>
[Serializable, NetSerializable]
public sealed class ThavenMoodsBuiState(List<ThavenMood> sharedMoods) : BoundUserInterfaceState
{
    public readonly List<ThavenMood> SharedMoods = sharedMoods;
}
