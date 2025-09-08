using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._Ronstation.Vampire.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
            "HumanoidAppearance",
        ],
    };
    [DataField, AutoNetworkedField]
    public SoundSpecifier? FeedNoise = new SoundPathSpecifier("/Audio/Items/drink.ogg");

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(10);

    /// <summary>
    /// How long it takes to drink this yourself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay = 1;

    /// <summary>
    /// The damage profile for a single tick of feed damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerTick = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Bloodloss", 6},
            { "Piercing", 2 },
            { "Slash", 2 }
        },
    };

    /// <summary>
    /// The damage profile for when the target's blood volume passes under the execution threshold
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier ExecuteDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Bloodloss", 60},
            { "Piercing", 20 },
            { "Slash", 20 }
        },
    };

   public override bool SendOnlyToOwner => true;
}