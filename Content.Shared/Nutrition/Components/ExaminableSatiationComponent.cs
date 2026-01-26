using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

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
public sealed partial class SatiationExaminationPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SatiationExaminationPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Dictionary of satiation thresholds to LocIds of the messages to display.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(DictionarySerializer<string, LocId>))]
    public Dictionary<string, LocId> Descriptions = [];

    /// <summary>
    /// LocId of a fallback message to display if the entity has no <see cref="SatiationComponent"/>, or does not have
    /// the satiation associated with this definition.
    /// </summary>
    [DataField(required: true)]
    public LocId NotApplicable;
}
