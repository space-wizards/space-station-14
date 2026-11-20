using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Kitchen;

/// <summary>
/// Integration tests related to microwaves and microwave recipes.
/// </summary>
public sealed class MicrowaveRecipeTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly ServerMicrowaveSystem _microwave = null!;
    [SidedDependency(Side.Server)] private readonly SharedContainerSystem _container = null!;
    [SidedDependency(Side.Server)] private readonly SolutionContainerSystem _solution = null!;

    private static readonly string[] Recipes = GameDataScrounger.PrototypesOfKind<MicrowaveMealRecipePrototype>();
    private static readonly EntProtoId MicrowavePrototype = "KitchenMicrowave";
    private static readonly EntProtoId PowerProvider = "APCBasic";
    private const uint MaxSeconds = 30;

    private const string TestReagent = "TestReagent";
    private const string TestReagentContainer = "TestContainer";
    private const string SolutionName = "SolutionName";
    private const string TestSolid = "TestSolid";

    [TestPrototypes]
    private const string Prototypes = $@"
    - type: microwaveMealRecipe
      id: ReagentTestRecipe
      result: {TestSolid}
      reagents:
        {TestReagent}: 6
      solids:
        {TestSolid}: 1

    - type: reagent
      id: {TestReagent}
      name: reagent-name-raw-egg
      desc: reagent-desc-raw-egg
      physicalDesc: reagent-physical-desc-mucus-like

    - type: reagent
      id: TestFailReagent
      name: reagent-name-raw-egg
      desc: reagent-desc-raw-egg
      physicalDesc: reagent-physical-desc-mucus-like

    - type: reaction
      id: TestEggCooking
      minTemp: 301
      reactants:
        {TestReagent}:
          amount: 1
      products:
        TestFailReagent: 0.5

    - type: entity
      id: {TestReagentContainer}
      components:
      - type: Item
      - type: DrainableSolution
        solution: {SolutionName}
      - type: SolutionManager
      - type: Solution
        id: {SolutionName}
        solution:
          maxVol: 6
          reagents:
          - ReagentId: {TestReagent}
            Quantity: 6

    - type: entity
      id: {TestSolid}
      components:
      - type: Item
    ";

    [Test]
    [TestOf(typeof(ServerMicrowaveSystem))]
    [TestCaseSource(nameof(Recipes))]
    [Description("Checks whether a microwave recipe's ingredients will create that recipe in the microwave.")]
    public async Task AllRecipeIngredientsMakeRecipe(string protoKey)
    {
        var server = Pair.Server;

        // Spawn the microwave we will use for our recipes.
        var microwave = await Spawn(MicrowavePrototype);
        var microwaveString = SEntMan.ToPrettyString(microwave);

        await server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<MicrowaveComponent>(microwave),
                $"Microwave entity {microwaveString} lacks a {nameof(MicrowaveComponent)}!");

            // Get the parameters we need to make this recipe.
            var proto = SProtoMan.Index<MicrowaveMealRecipePrototype>(protoKey);
            var maxPortions = MaxSeconds / proto.Time;

            // Ensure this recipe is provided to the microwave if this is a secret recipe.
            if (proto.SecretRecipe)
            {
                var recipeProvider = SEntMan.EnsureComponent<FoodRecipeProviderComponent>(microwave);
                recipeProvider.ProvidedRecipes.Add(protoKey);
            }

            // First, test that a single portion works.
            ValidateRecipePortions(proto, 1, microwave);

            // Then, test that making multiple portions of the same recipe works.
            if (maxPortions > 1)
                ValidateRecipePortions(proto, maxPortions, microwave);
        });
    }

    private void ValidateRecipePortions(MicrowaveMealRecipePrototype prototype, uint portions, EntityUid microwave)
    {
        var ingredients = prototype.Ingredients * portions;
        var cookTime = prototype.Time * portions;
        var portionedRecipe = _microwave.GetRecipe(microwave, ingredients, cookTime);
        var microwaveString = SEntMan.ToPrettyString(microwave);
        var recipeDebugString = $"Ingredients for {nameof(MicrowaveMealRecipePrototype)} {prototype.ID}";

        using (Assert.EnterMultipleScope())
        {
            // Tried to get a recipe for these ingredients, but no valid recipe was found.
            Assert.That(portionedRecipe, Is.Not.Null,
                $"{recipeDebugString} did not resolve to a recipe in {microwaveString} in {portions} portions!");

            var recipe = portionedRecipe.Value.Recipe;
            var count = portionedRecipe.Value.Count;

            // Resulted in a different recipe instead.
            Assert.That(recipe, Is.EqualTo(prototype.ID),
                $"{recipeDebugString} resulted in an incorrect recipe for {microwaveString} in {portions} portions!");

            // Recipe portion count does not match the amount we're trying to make.
            Assert.That(count, Is.EqualTo(portions),
                $"{recipeDebugString} resulted in {count} recipe portions for {microwaveString}! Expected: {portions}");
        }
    }

    [Test]
    [TestOf(typeof(ServerMicrowaveSystem))]
    [Description("Tests that a recipe with a reagent ingredient doesn't change mid-cook.")]
    public async Task ReagentDupeTest()
    {
        // Needs a map for power and to be anchored
        var map = await Pair.CreateTestMap();

        var m = await SpawnAtPosition(MicrowavePrototype, map.GridCoords);
        var microwave = (Owner: m, Comp: SComp<MicrowaveComponent>(m));

        await SpawnAtPosition(PowerProvider, map.GridCoords);
        await Server.WaitRunTicks(1);
        Assume.That(_microwave.IsPowered(microwave.Owner, SEntMan), "Microwave is not powered.");

        var solid = await Spawn(TestSolid);
        var reagent = await Spawn(TestReagentContainer);

        await Server.WaitPost(() =>
        {
            _microwave.SetCookTime(microwave, 5);

            var container = _container.GetContainer(microwave.Owner, microwave.Comp.ContainerId);
            _container.Insert(reagent, container);
            _container.Insert(solid, container);

            _microwave.StartCooking(microwave, null);
            Assume.That(SComp<ActiveMicrowaveComponent>(microwave.Owner).PortionedRecipe, Is.Not.Null, "Microwave failed to select a recipe.");
        });

        await Pair.RunSeconds(5.1f); // Run for the recipe's cook duration, plus some grace for post-cook
        Assume.That(_microwave.IsPowered(microwave.Owner, SEntMan), "Microwave became unpowered while cooking.");
        Assume.That(_microwave.IsActiveMicrowave(microwave.Owner), Is.False, "Microwave has not finished cooking.");

        Assume.That(_solution.TryGetSolution(reagent, SolutionName, out _, out var sol), Is.True, "Failed to get solution.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SEntMan.EntityExists(solid), Is.False, "Recipe solid was not consumed.");
            Assert.That(sol!.Volume, Is.EqualTo(FixedPoint2.Zero), "Reagent changed mid-cook or was not consumed.");
        }
    }
}
