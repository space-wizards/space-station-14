using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Chemistry;

/// <summary>
/// An abstract solutions test which spawns all entities with a generic component.
/// Attempts to resolve that solution via an abstract string matching method.
/// The reason this doesn't use interfaces is that string matching is obsolete and going to be deprecated soon.
/// It's not worth doubling the breaking changes just to make an obsolete behavior prettier.
/// </summary>
/// <typeparam name="T">A Component that tries to match to a solution via a string</typeparam>
[TestOf(typeof(SolutionComponent)), TestOf(typeof(SolutionManagerComponent)),
 TestOf(typeof(SolutionContainerSystem))]
public abstract partial class SolutionStringResolveTest<T> : GameTest where T : IComponent
{
    [SidedDependency(Side.Server)] protected SharedSolutionContainerSystem SSolutionSystem = default!;

    private static string[] _prototypes = GameDataScrounger.EntitiesWithComponent(CalculateComponentName(typeof(T)));

    [Test]
    [TestCaseSource(nameof(_prototypes))]
    [Description("Ensures all entities can resolve their attached solution.")]
    [RunOnSide(Side.Server)]
    public void SolutionStringResolve(string proto)
    {
        var uid = SSpawn(proto);
        var comp = SComp<T>(uid);
        Assert.That(SSolutionSystem.TryGetSolution(uid, GetTargetName((uid, comp)), out var solution, true), Is.True, $"{SToPrettyString(uid)} failed to resolve solution for {typeof(T)}");
        Test((uid, comp), solution!.Value);
    }

    /// <summary>
    /// Returns the string of the solution the tested component is looking for.
    /// </summary>
    /// <param name="entity">Entity with an attached solution that needs to be resolved.</param>
    /// <returns>The string of the solution we're looking for</returns>
    protected abstract string GetTargetName(Entity<T> entity);

    /// <summary>
    /// Called after the solution attached to the tested generic component is resolved.
    /// This allows you to test specific behaviors between the attached entity its solution if needed.
    /// This kind of solution behavior is obsolete so only fill this in if you truly have to.
    /// </summary>
    /// <param name="entity">Entity with the attached solution</param>
    /// <param name="solution">Attached solution</param>
    protected virtual void Test(Entity<T> entity, Entity<SolutionComponent> solution)
    {

    }

    /// <remarks>
    /// Copied directly from ComponentFactory because comp factory's method isn't public.
    /// TODO: PR this method to public in engine!
    /// </remarks>
    private static string CalculateComponentName(Type type)
    {
        // Attributes can use any name they want, they are for bypassing the automatic names
        // If a parent class has this attribute, a child class will use the same name, unless it also uses this attribute
        if (Attribute.GetCustomAttribute(type, typeof(ComponentProtoNameAttribute)) is ComponentProtoNameAttribute attribute)
            return attribute.PrototypeName;

        const string component = "Component";
        var typeName = type.Name;
        if (!typeName.EndsWith(component))
        {
            throw new InvalidComponentNameException($"Component {type} must end with the word Component");
        }

        string name = typeName[..^component.Length];
        const string client = "Client";
        const string server = "Server";
        const string shared = "Shared";
        if (typeName.StartsWith(client, StringComparison.Ordinal))
        {
            name = typeName[client.Length..^component.Length];
        }
        else if (typeName.StartsWith(server, StringComparison.Ordinal))
        {
            name = typeName[server.Length..^component.Length];
        }
        else if (typeName.StartsWith(shared, StringComparison.Ordinal))
        {
            name = typeName[shared.Length..^component.Length];
        }
        DebugTools.Assert(name != String.Empty, $"Component {type} has invalid name {type.Name}");
        return name;
    }
}

[TestOf(typeof(DrainableSolutionComponent))]
public sealed partial class DrainableSolutionTest : SolutionStringResolveTest<DrainableSolutionComponent>
{
    protected override string GetTargetName(Entity<DrainableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(DumpableSolutionComponent))]
public sealed partial class DumpableSolutionTest : SolutionStringResolveTest<DumpableSolutionComponent>
{
    protected override string GetTargetName(Entity<DumpableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(DrawableSolutionComponent))]
public sealed partial class DrawableSolutionTest : SolutionStringResolveTest<DrawableSolutionComponent>
{
    protected override string GetTargetName(Entity<DrawableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(EdibleComponent))]
public sealed partial class EdibleSolutionTest : SolutionStringResolveTest<EdibleComponent>
{
    protected override string GetTargetName(Entity<EdibleComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(ExaminableSolutionComponent))]
public sealed partial class ExaminableSolutionTest : SolutionStringResolveTest<ExaminableSolutionComponent>
{
    protected override string GetTargetName(Entity<ExaminableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(InjectableSolutionComponent))]
public sealed partial class InjectableSolutionTest : SolutionStringResolveTest<InjectableSolutionComponent>
{
    protected override string GetTargetName(Entity<InjectableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(InjectorComponent))]
public sealed partial class InjectorSolutionTest : SolutionStringResolveTest<InjectorComponent>
{
    protected override string GetTargetName(Entity<InjectorComponent> entity)
    {
        return entity.Comp.SolutionName;
    }
}

[TestOf(typeof(MixableSolutionComponent))]
public sealed partial class MixableSolutionTest : SolutionStringResolveTest<MixableSolutionComponent>
{
    protected override string GetTargetName(Entity<MixableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(RefillableSolutionComponent))]
public sealed partial class RefillableSolutionTest : SolutionStringResolveTest<RefillableSolutionComponent>
{
    protected override string GetTargetName(Entity<RefillableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(ScoopableSolutionComponent))]
public sealed partial class ScoopableSolutionTest : SolutionStringResolveTest<ScoopableSolutionComponent>
{
    protected override string GetTargetName(Entity<ScoopableSolutionComponent> entity)
    {
        return entity.Comp.Solution;
    }
}

[TestOf(typeof(SolutionContainerVisualsComponent))]
public sealed partial class SolutionContainerVisualsSolutionTest : SolutionStringResolveTest<SolutionContainerVisualsComponent>
{
    protected override string GetTargetName(Entity<SolutionContainerVisualsComponent> entity)
    {
        return entity.Comp.SolutionName;
    }
}

[TestOf(typeof(SolutionSpikerComponent))]
public sealed partial class SolutionSpikerSolutionTest : SolutionStringResolveTest<SolutionSpikerComponent>
{
    protected override string GetTargetName(Entity<SolutionSpikerComponent> entity)
    {
        return entity.Comp.SourceSolution;
    }
}
