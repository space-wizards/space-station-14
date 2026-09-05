using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Store.Components;

/// <summary>
/// Generates a specific type of currency that can be collected by stores.
/// Either by a store held on an item or on the player entity itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class StoreCurrencyGeneratorComponent : Component
{
    /// <summary>
    /// Whether this generator is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The currency being generated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CurrencyPrototype> Currency = "Telecrystal";

    /// <summary>
    /// The current amount stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount;

    /// <summary>
    /// The maximum amount that can be stored at once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxAmount = 5;

    /// <summary>
    /// The amount of the currency generated every <see cref="GenerationDelay"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 GeneratedAmount = 1;

    /// <summary>
    /// Whitelist the store has to pass to be able to pull from this generator.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist the store has to pass to be able to pull from this generator.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The time between currency being generated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan GenerationDelay = TimeSpan.FromSeconds(120);

    /// <summary>
    /// The time at which the next currency will be generated.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextGenerationTime = TimeSpan.Zero;

    /// <summary>
    /// LocId of the text used for the verb the store sees when looking at this entity.
    /// </summary>
    [DataField]
    public LocId Verb = "store-generator-verb";

    /// <summary>
    /// Description for the <see cref="Verb"/> verb.
    /// </summary>
    [DataField]
    public LocId VerbDescription = "store-generator-verb-description";

    /// <summary>
    /// Description to show to the user/store when it is collected from.
    /// </summary>
    [DataField]
    public LocId CollectPopup = "store-generator-collect-popup";
}
