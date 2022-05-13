using Content.Shared.Chemistry.Reagent;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffectConditions;

[UsedImplicitly]
public sealed class HasTag : ReagentEffectCondition
{
    [DataField("tag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string Tag = default!;

    [DataField("invert")]
    public bool Invert = false;

    public override bool Condition(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<TagComponent>(args.SolutionEntity, out var tag))
            return EntitySystem.Get<TagSystem>().HasTag(tag, Tag) ^ Invert;

        return false;
    }
}
