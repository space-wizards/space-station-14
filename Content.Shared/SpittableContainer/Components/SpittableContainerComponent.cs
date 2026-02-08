using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SpittableContainer.Components;

/// <summary>
/// Grants the entity actions to swallow and spit items out of a provided container).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SpittableContainerSystem))]
public sealed partial class SpittableContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? SwallowActionPrototype = "ActionContainerSwallow";

    [DataField, AutoNetworkedField]
    public EntityUid? SwallowActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId? SpitContainerActionPrototype = "ActionContainerSpit";

    [DataField, AutoNetworkedField]
    public EntityUid? SpitContainerActionEntity;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundEat = new SoundCollectionSpecifier("eating");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundSpit = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };

    /// <summary>
    /// The popup to show when the entity spits out items.
    /// Will not show anything if null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? SpitPopup = "spittable-container-spit";

    /// <summary>
    /// The popup to show when the entity can't swallow an item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SwallowFailPopup = "spittable-container-swallow-fail";

    /// <summary>
    /// Time it takes to swallow an item into the container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SwallowTime = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Time it takes to spit items out of the container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SpitTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Where the entities go when it devours them, empties when it is butchered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Storage = "storagebase";

    public Container? Container = default!;
}

