using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._MACRO.Species.Kodepiia.Consume.Components;

/// <summary>
/// Entities with the component gain the ability to "consume" other entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsumeActionComponent : Component
{
    /// <summary>
    /// The consume action entity itself.
    /// </summary>
    [DataField]
    public EntityUid? ConsumeAction;

    /// <summary>
    /// The Id of the action.
    /// </summary>
    [DataField]
    public string? ConsumeActionId;

    /// <summary>
    /// Damage dealt to target entity
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Whether or not the consumer can eat corpses that are rotten.
    /// </summary>
    [DataField]
    public bool CanEatRotten = true;

    /// <summary>
    ///     The base time of the doAfter for consuming a mob.
    /// </summary>
    /// <remarks>
    ///     This is multiplied by the ratio of the target's mass to the consumer's mass;
    ///     for instance, a consumer will bite smaller mobs faster, and vice versa.
    /// </remarks>
    [DataField]
    public TimeSpan BaseConsumeTime = TimeSpan.FromSeconds(10.0f);

    /// <summary>
    /// reagent ingested when eating.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> FoodReagentPrototype = "UncookedAnimalProteins";

    /// <summary>
    /// Percentage of toxin when eating a rotten corpse. Do not set a number less than 0 or more than 1
    /// </summary>
    [DataField]
    public float ToxinRatio = 0.5f;

    /// <summary>
    /// toxin ingested when eating a rotten corpse.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Toxin = "GastroToxin";

    /// <summary>
    /// Solution Container to eat from! Yummy!
    /// </summary>
    [DataField]
    public string SolutionToDrinkFrom = BloodstreamComponent.DefaultBloodSolutionName;

    /// <summary>
    /// Body mass is multiplied by this to get the amount of
    /// food reagent you should get when eating a corpse.
    /// </summary>
    [DataField]
    public float MeatMultiplier = 0.25f;

    /// <summary>
    /// Percentage of Bloodstream to drink when consuming.
    /// </summary>
    [DataField]
    public float PortionDrunk = 0.1f;

    /// <summary>
    /// How much of the entity we want to consume (keep in mind the default gib threshold is 12)
    /// </summary>
    [DataField]
    public float ConsumptionAmount = 1f;

    /// <summary>
    /// Sound that is played when the the victim is consumed.
    /// </summary>
    [DataField]
    public SoundSpecifier ConsumptionSound = new SoundCollectionSpecifier("gib")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// LocId of the failure popup that occurs when consuming is blocked.
    /// </summary>
    [DataField]
    public LocId ConsumeFailByBlock = "consume-fail-blocked";

    /// <summary>
    /// LocId of the failure popup that occurs when the victim is inedible.
    /// </summary>
    [DataField]
    public LocId ConsumeFailByInedible = "consume-fail-inedible";

    /// <summary>
    /// LocId of the failure popup that occurs when consuming isn't incapacitiated.
    /// </summary>
    [DataField]
    public LocId ConsumeFailByIncapacitated = "consume-fail-incapacitated";

    /// <summary>
    /// LocId of the failure popup that occurs when the consumer is full.
    /// </summary>
    [DataField]
    public LocId ConsumeFailByFullStomach = "ingestion-you-cannot-ingest-any-more";

    /// <summary>
    /// The verb used for consuming a target. E.g.: "You cannot [verb] this target!"
    /// </summary>
    [DataField]
    public LocId ConsumeVerb = "edible-verb-food";

    /// <summary>
    /// LocId of the popup that only shows up to the consumer when they consume something.
    /// </summary>
    [DataField]
    public LocId? PopupSelfStart;

    /// <summary>
    /// LocId of the popup that shows up to everyone but the consumer when they consume something.
    /// </summary>
    [DataField]
    public LocId? PopupOthersStart;

    /// <summary>
    /// LocId of the popup that only shows up to the consumer after they consume something.
    /// </summary>
    [DataField]
    public LocId? PopupSelfEnd;

    /// <summary>
    /// LocId of the popup that shows up to everyone but the consumer after they consume something.
    /// </summary>
    [DataField]
    public LocId? PopupOthersEnd;

    /// <summary>
    /// Whitelist of consumable entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist of consumable entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
