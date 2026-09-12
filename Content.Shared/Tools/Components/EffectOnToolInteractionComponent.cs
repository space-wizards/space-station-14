using Content.Shared.EntityEffects;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.Tools.Components;

/// <summary>
/// This component causes effects to be applied when a tool is used on this component's owner. The effects are
/// applied with the <c>target</c> being the owner of this component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EffectOnToolInteractionSystem))]
public sealed partial class EffectOnToolInteractionComponent : Component
{
    /// <summary>
    /// A mapping of tool qualities to the effects to be applied.
    /// </summary>
    [DataField(
        required: true,
        customTypeSerializer:
        typeof(DictionarySerializer<ProtoId<ToolQualityPrototype>, List<EffectOnToolInteractionEffect>>)
    )]
    public Dictionary<ProtoId<ToolQualityPrototype>, List<EffectOnToolInteractionEffect>> Effects;
}

/// <summary>
/// Effects applied by <seealso cref="EffectOnToolInteractionComponent"/>.
/// </summary>
///
/// <param name="Target">
/// The effects to apply to the target (the owner of the <seealso cref="EffectOnToolInteractionComponent"/>).
/// </param>
/// <param name="Tool">
/// The effects to apply to the tool being used.
/// </param>
[DataRecord]
public sealed partial record EffectOnToolInteractionEffect(
    List<EntityEffect>? Target,
    List<EntityEffect>? Tool
);
