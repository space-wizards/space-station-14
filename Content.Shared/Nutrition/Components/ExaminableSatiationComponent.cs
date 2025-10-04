using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Component which specifies what <see cref="ExaminableSatiationComponent"/> shows in examine descriptions.
/// </summary>
/// <seealso cref="SatiationExaminationPrototype"/>
/// <seealso cref="ExaminableSatiationSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ExaminableSatiationSystem))]
public sealed partial class ExaminableSatiationComponent : Component
{
    /// <summary>
    /// A dictionary which relates <see cref="SatiationExaminationPrototype"/> to the
    /// <see cref="SatiationTypePrototype">type</see> it describes.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, ProtoId<SatiationExaminationPrototype>> Satiations = [];
}

/// <summary>
/// A definitions of how to describe a satiation's status in examine messages.
/// </summary>
/// <seealso cref="ExaminableSatiationComponent"/>
[Prototype]
public sealed class SatiationExaminationPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SatiationExaminationPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// Dictionary of satiation thresholds to LocIds of the messages to display.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<SatiationThreshold, LocId> Descriptions = [];

    /// <summary>
    /// LocId of a fallback message to display if the entity has no <see cref="SatiationComponent"/>, does not have the
    /// satiation associated with this definition, or does not have a value in <see cref="Descriptions"/> for the
    /// current threshold.
    /// </summary>
    [DataField(required: true)]
    public LocId NotApplicable;

    /// <summary>
    /// Gets the appropriate examine text's <see cref="LocId"/> for <paramref name="threshold"/>. If null or no such
    /// text is in <see cref="Descriptions"/>, returns <see cref="NotApplicable"/>.
    /// </summary>
    public LocId GetDescriptionOrDefault(SatiationThreshold? threshold)
    {
        return threshold is { } t
            ? Descriptions.GetValueOrDefault(t, NotApplicable)
            : NotApplicable;
    }
}
