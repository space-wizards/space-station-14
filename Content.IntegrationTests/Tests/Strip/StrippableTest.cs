using Content.Client.Interaction;
using Content.IntegrationTests.Tests.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Strip;

public sealed class StrippableTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task DragDropOpensStrip()
    {
        // Spawn one tile away
        TargetCoords = SEntMan.GetNetCoordinates(new EntityCoordinates(MapData.MapUid, 1, 0));
        await SpawnTarget("MobHuman");

        var userInterface = Comp<UserInterfaceComponent>(Target);
        Assert.That(userInterface.Actors.Count == 0);

        // screenCoordinates diff needs to be larger than DragDropSystem._deadzone
        var screenX = CEntMan.System<DragDropSystem>().Deadzone + 1f;

        // Start drag
        await SetKey(EngineKeyFunctions.Use,
            BoundKeyState.Down,
            TargetCoords,
            Target,
            screenCoordinates: new ScreenCoordinates(screenX, 0f, WindowId.Main));

        await RunTicks(5);

        // End drag
        await SetKey(EngineKeyFunctions.Use,
            BoundKeyState.Up,
            PlayerCoords,
            Player,
            screenCoordinates: new ScreenCoordinates(0f, 0f, WindowId.Main));

        await RunTicks(5);

        Assert.That(userInterface.Actors.Count > 0);
    }
}
