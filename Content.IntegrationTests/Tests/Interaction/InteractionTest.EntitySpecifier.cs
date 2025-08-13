#nullable enable
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.IntegrationTests.Tests.Interaction;

public abstract partial class InteractionTest
{
    /// <summary>
    /// Utility class for working with prototypes ids that may refer to stacks or entities.
    /// </summary>
    /// <remarks>
    /// Intended to make tests easier by removing ambiguity around "SheetSteel1", "SheetSteel", and "Steel". All three
    /// should be treated identically by interaction tests.
    /// </remarks>
    protected sealed class EntitySpecifier
    {
        /// <summary>
        /// Either the stack or entity prototype for this entity. Stack prototypes take priority.
        /// </summary>
        public string Prototype;

        /// <summary>
        /// The quantity. If the entity has a stack component, this is the total stack quantity.
        /// Otherwise this is the number of entities.
        /// </summary>
        /// <remarks>
        /// If used for spawning and this number is larger than the max stack size, only a single stack will be spawned.
        /// </remarks>
        public int Quantity;

        /// <summary>
        /// If true, a check has been performed to see if the prototype is an entity prototype with a stack component,
        /// in which case the specifier was converted into a stack-specifier
        /// </summary>
        public bool Converted;

        public EntitySpecifier(string prototype, int quantity, bool converted = false)
        {
            Assert.That(quantity, Is.GreaterThan(0));
            Prototype = prototype;
            Quantity = quantity;
            Converted = converted;
        }

        public static implicit operator EntitySpecifier(string prototype)
            => new(prototype, 1);

        public static implicit operator EntitySpecifier((string, int) tuple)
            => new(tuple.Item1, tuple.Item2);

        /// <summary>
        /// Convert applicable entity prototypes into stack prototypes.
        /// </summary>
        public async Task ConvertToStack(IPrototypeManager protoMan, IComponentFactory factory, ServerIntegrationInstance server)
        {
            if (Converted)
                return;

            Converted = true;

            if (string.IsNullOrWhiteSpace(Prototype))
                return;

            if (protoMan.HasIndex<StackPrototype>(Prototype))
                return;

            if (!protoMan.TryIndex<EntityPrototype>(Prototype, out var entProto))
            {
                Assert.Fail($"Unknown prototype: {Prototype}");
                return;
            }

            StackComponent? stack = null;
            await server.WaitPost(() =>
            {
                entProto.TryGetComponent(factory.GetComponentName<StackComponent>(), out stack);
            });

            if (stack != null)
                Prototype = stack.StackTypeId;
        }
    }

    protected async Task<EntityUid> SpawnEntity(EntitySpecifier spec, EntityCoordinates coords)
    {
        EntityUid uid = default!;
        if (ProtoMan.TryIndex<StackPrototype>(spec.Prototype, out var stackProto))
        {
            await Server.WaitPost(() =>
            {
                uid = SEntMan.SpawnEntity(stackProto.Spawn, coords);
                Stack.SetCount(uid, spec.Quantity);
            });
            return uid;
        }

        if (!ProtoMan.TryIndex<EntityPrototype>(spec.Prototype, out var entProto))
        {
            Assert.Fail($"Unknown prototype: {spec.Prototype}");
            return default;
        }

        StackComponent? stack = null;
        await Server.WaitPost(() =>
        {
            entProto.TryGetComponent(Factory.GetComponentName<StackComponent>(), out stack);
        });

        if (stack != null)
            return await SpawnEntity((stack.StackTypeId, spec.Quantity), coords);

        Assert.That(spec.Quantity, Is.EqualTo(1), "SpawnEntity only supports returning a singular entity");
        await Server.WaitPost(() => uid = SEntMan.SpawnAtPosition(spec.Prototype, coords));
        return uid;
    }

    /// <summary>
    /// Convert an entity-uid to a matching entity specifier. Useful when doing entity lookups & checking that the
    /// right quantity of entities/materials were produced. Returns null if passed an entity with a null prototype.
    /// </summary>
    protected EntitySpecifier? ToEntitySpecifier(EntityUid uid)
    {
        if (SEntMan.TryGetComponent(uid, out StackComponent? stack))
            return new EntitySpecifier(stack.StackTypeId, stack.Count) { Converted = true };

        var meta = SEntMan.GetComponent<MetaDataComponent>(uid);

        if (meta.EntityPrototype is null)
            return null;

        return new(meta.EntityPrototype.ID, 1) { Converted = true };
    }
}
