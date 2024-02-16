#nullable enable
using System.Numerics;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Interaction;

/// <summary>
/// This is a variation of <see cref="InteractionTest"/> that sets up the player with a normal human entity and a simple
/// linear grid with gravity and an atmosphere. It is intended to make it easier to test interactions that involve
/// walking (e.g., slipping or climbing tables).
/// </summary>
public abstract class MovementTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    /// <summary>
    ///     Number of tiles to add either side of the player.
    /// </summary>
    protected virtual int Tiles => 3;

    /// <summary>
    ///     If true, the tiles at the ends of the grid will have a wall placed on them to avoid players moving off grid.
    /// </summary>
    protected virtual bool AddWalls => true;

    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        var pCoords = SEntMan.GetCoordinates(PlayerCoords);

        for (var i = -Tiles; i <= Tiles; i++)
        {
            await SetTile(Plating, SEntMan.GetNetCoordinates(pCoords.Offset(new Vector2(i, 0))), MapData.MapGrid);
        }
        AssertGridCount(1);

        if (AddWalls)
        {
            await SpawnEntity("WallSolid", pCoords.Offset(new Vector2(-Tiles, 0)));
            await SpawnEntity("WallSolid", pCoords.Offset(new Vector2(Tiles, 0)));
        }

        await AddGravity();
        await AddAtmosphere();
    }

    /// <summary>
    ///     Get the relative horizontal between two entities. Defaults to using the target & player entity.
    /// </summary>
    protected float Delta(NetEntity? target = null, NetEntity? other = null)
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return 0;
        }

        var delta = Transform.GetWorldPosition(SEntMan.GetEntity(target.Value)) - Transform.GetWorldPosition(SEntMan.GetEntity(other ?? Player));
        return delta.X;
    }
}

