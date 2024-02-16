#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Interaction;

public abstract partial class InteractionTest
{
    /// <summary>
    /// Data structure for representing a collection of <see cref="EntitySpecifier"/>s.
    /// </summary>
    protected sealed class EntitySpecifierCollection
    {
        public Dictionary<string, int> Entities = new();

        /// <summary>
        /// If true, a check has been performed to see if the prototypes correspond to entity prototypes with a stack
        /// component, in which case the specifier was converted into a stack-specifier
        /// </summary>
        public bool Converted;

        public EntitySpecifierCollection()
        {
            Converted = true;
        }

        public EntitySpecifierCollection(IEnumerable<EntitySpecifier> ents)
        {
            Converted = true;
            foreach (var ent in ents)
            {
                Add(ent);
            }
        }

        public static implicit operator EntitySpecifierCollection(string prototype)
        {
            var result = new EntitySpecifierCollection();
            result.Add(prototype, 1);
            return result;
        }

        public static implicit operator EntitySpecifierCollection((string, int) tuple)
        {
            var result = new EntitySpecifierCollection();
            result.Add(tuple.Item1, tuple.Item2);
            return result;
        }

        public void Remove(EntitySpecifier spec)
        {
            Add(new EntitySpecifier(spec.Prototype, -spec.Quantity, spec.Converted));
        }

        public void Add(EntitySpecifier spec)
        {
            Add(spec.Prototype, spec.Quantity, spec.Converted);
        }

        public void Add(string id, int quantity, bool converted = false)
        {
            Converted &= converted;

            if (!Entities.TryGetValue(id, out var existing))
            {
                if (quantity != 0)
                    Entities.Add(id, quantity);
                return;
            }

            var newQuantity = quantity + existing;
            if (newQuantity == 0)
                Entities.Remove(id);
            else
                Entities[id] = newQuantity;
        }

        public void Add(EntitySpecifierCollection collection)
        {
            var converted = Converted && collection.Converted;
            foreach (var (id, quantity) in collection.Entities)
            {
                Add(id, quantity);
            }
            Converted = converted;
        }

        public void Remove(EntitySpecifierCollection collection)
        {
            var converted = Converted && collection.Converted;
            foreach (var (id, quantity) in collection.Entities)
            {
                Add(id, -quantity);
            }
            Converted = converted;
        }

        public EntitySpecifierCollection Clone()
        {
            return new EntitySpecifierCollection()
            {
                Entities = Entities.ShallowClone(),
                Converted = Converted
            };
        }

        /// <summary>
        /// Convert applicable entity prototypes into stack prototypes.
        /// </summary>
        public void ConvertToStacks(IPrototypeManager protoMan, IComponentFactory factory)
        {
            if (Converted)
                return;

            HashSet<string> toRemove = new();
            List<(string, int)> toAdd = new();
            foreach (var (id, quantity) in Entities)
            {

                if (protoMan.HasIndex<StackPrototype>(id))
                    continue;

                if (!protoMan.TryIndex<EntityPrototype>(id, out var entProto))
                {
                    Assert.Fail($"Unknown prototype: {id}");
                    continue;
                }

                if (!entProto.TryGetComponent<StackComponent>(factory.GetComponentName(typeof(StackComponent)),
                        out var stackComp))
                {
                    continue;
                }

                toRemove.Add(id);
                toAdd.Add((stackComp.StackTypeId, quantity));
            }

            foreach (var id in toRemove)
            {
                Entities.Remove(id);
            }

            foreach (var (id, quantity) in toAdd)
            {
                Add(id, quantity);
            }

            Converted = true;
        }
    }

    protected EntitySpecifierCollection ToEntityCollection(IEnumerable<EntityUid> entities)
    {
        var collection = new EntitySpecifierCollection(entities
            .Select(ToEntitySpecifier)
            .OfType<EntitySpecifier>());
        Assert.That(collection.Converted);
        return collection;
    }
}
