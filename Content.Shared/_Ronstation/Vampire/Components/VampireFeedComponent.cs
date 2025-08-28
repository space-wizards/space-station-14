using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared._Ronstation.Vampire.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._Ronstation.Vampire.Components;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(VampireFeedSystem))]
public sealed partial class VampireFeedComponent : Component
{
    /// <summary>
    /// The Action for vampire's feeding
    /// </summary>
    [DataField]
    public EntProtoId? VampireFeedAction = "ActionVampireFeed";

    /// <summary>
    /// The action entity associated with feeding
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? VampireFeedActionEntity;

    /// <summary>
    /// The whitelist of targets for feeding
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components =
        [
            "MobState",
            "HumanoidAppearance",
        ],
    };
    [DataField, AutoNetworkedField]
    public SoundSpecifier? FeedNoise = new SoundPathSpecifier("/Audio/Items/drink.ogg");

    /// <summary>
    /// The time between damage ticks
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DamageTimeBetweenTicks = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// How long it takes to drink this yourself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay = 1;

    /// <summary>
    /// If true, trying to drink when empty will not handle the event.
    /// This means other systems such as equipping on use can run.
    /// Example usecase is the bucket.
    /// </summary>
    [DataField]
    public bool IgnoreEmpty;

    /// <summary>
    /// The damage profile for a single tick of feed damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerTick = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Bloodloss", 10},
            { "Piercing", 10 },
        },
    };
}