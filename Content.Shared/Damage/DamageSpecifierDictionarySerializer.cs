using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Damage;

//todo writing
public sealed class DamageSpecifierDictionarySerializer : ITypeReader<Dictionary<string, FixedPoint2>, MappingDataNode>
{
    private ITypeValidator<Dictionary<string, FixedPoint2>, MappingDataNode> _damageTypeSerializer = new PrototypeIdDictionarySerializer<FixedPoint2, DamageTypePrototype>();
    private ITypeValidator<Dictionary<string, FixedPoint2>, MappingDataNode> _damageGroupSerializer = new PrototypeIdDictionarySerializer<FixedPoint2, DamageGroupPrototype>();

    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var vals = new Dictionary<ValidationNode, ValidationNode>();
        if (node.TryGet<MappingDataNode>("types", out var typesNode))
        {
            vals.Add(new ValidatedValueNode(new ValueDataNode("types")), _damageTypeSerializer.Validate(serializationManager, typesNode, dependencies, context));
        }

        if (node.TryGet<MappingDataNode>("groups", out var groupsNode))
        {
            vals.Add(new ValidatedValueNode(new ValueDataNode("groups")), _damageGroupSerializer.Validate(serializationManager, groupsNode, dependencies, context));
        }

        return new ValidatedMappingNode(vals);
    }

    public Dictionary<string, FixedPoint2> Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Dictionary<string, FixedPoint2>>? instanceProvider = null)
    {
        var dict = instanceProvider != null ? instanceProvider() : new();
        // Add all the damage types by just copying the type dictionary (if it is not null).
        if (node.TryGet("types", out var typesNode))
        {
            serializationManager.Read(typesNode, instanceProvider: () => dict, notNullableOverride: true);
        }

        if (!node.TryGet("groups", out var groupsNode))
            return dict;

        // Then resolve damage groups and add them
        var prototypeManager = dependencies.Resolve<IPrototypeManager>();
        foreach (var entry in serializationManager.Read<Dictionary<string, FixedPoint2>>(groupsNode, notNullableOverride: true))
        {
            if (!prototypeManager.TryIndex<DamageGroupPrototype>(entry.Key, out var group))
            {
                // This can happen if deserialized before prototypes are loaded.
                // i made this a warning bc it was failing tests -paul
                dependencies.Resolve<ILogManager>().RootSawmill.Error($"Unknown damage group given to DamageSpecifier: {entry.Key}");
                continue;
            }

            // Simply distribute evenly (except for rounding).
            // We do this by reducing remaining the # of types and damage every loop.
            var remainingTypes = group.DamageTypes.Count;
            var remainingDamage = entry.Value;
            foreach (var damageType in group.DamageTypes)
            {
                var damage = remainingDamage / FixedPoint2.New(remainingTypes);
                if (!dict.TryAdd(damageType, damage))
                {
                    // Key already exists, add values
                    dict[damageType] += damage;
                }
                remainingDamage -= damage;
                remainingTypes -= 1;
            }
        }

        return dict;
    }
}
