namespace Content.IntegrationTests.Tests.Stacks;

public static class StackTestPrototypes
{
    public const string StackPrototype = "TestStack";
    public const string StackEntEdible = "StackEntEdible";

    public const string StackEnt1 = "StackEnt1";
    private const string StackCount1 = "1";
    public const string StackEnt2 = "StackEnt2";
    private const string StackCount2 = "2";
    public const string StackEnt30 = "StackEnt30";
    private const string StackCount30 = "30"; // Also the maximum size of the test stack

    [TestPrototypes]
    public const string Prototypes =
        @$"
        - type: stack
          id: {StackPrototype}
          name: stack-steel
          spawn: {StackEnt1}
          maxCount: {StackCount30}

        - type: entity
          id: {StackEnt1}
          components:
          - type: Stack
            stackType: {StackPrototype}
            count: {StackCount1}
          - type: Item
          - type: Physics
            bodyType: Dynamic
          - type: Fixtures
            fixtures:
              fix1:
                shape:
                  !type:PhysShapeCircle
                  radius: 0.35
                layer:
                - Impassable

        - type: entity
          parent: {StackEnt1}
          id: {StackEnt2}
          components:
          - type: Stack
            count: {StackCount2}

        - type: entity
          parent: {StackEnt1}
          id: {StackEnt30}
          components:
          - type: Stack
            count: {StackCount30}

        - type: entity
          parent: {StackEnt1}
          id: {StackEntEdible}
          components:
          - type: Edible
          - type: Solution
            id: food
            solution:
              reagents:
              - ReagentId: Nothing
                Quantity: 5
        ";
}
