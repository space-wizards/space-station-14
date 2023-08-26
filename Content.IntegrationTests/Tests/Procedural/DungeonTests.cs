using System.Collections.Generic;
using Content.Server.Procedural;
using Content.Shared.Procedural;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Procedural;

[TestOf(typeof(DungeonSystem))]
public sealed class DungeonTests
{
    [Test]
    public async Task TestDungeonRoomPackBounds()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoManager = pair.Server.ResolveDependency<IPrototypeManager>();

        await pair.Server.WaitAssertion(() =>
        {
            var sizes = new HashSet<Vector2i>();

            foreach (var proto in protoManager.EnumeratePrototypes<DungeonRoomPrototype>())
            {
                sizes.Add(proto.Size);
                sizes.Add(new Vector2i(proto.Size.Y, proto.Size.X));
            }

            foreach (var pack in protoManager.EnumeratePrototypes<DungeonRoomPackPrototype>())
            {
                var rooms = new List<Box2>();

                for (var i = 0; i < pack.Rooms.Count; i++)
                {
                    var room = pack.Rooms[i];
                    var bounds = (Box2) room;

                    for (var j = 0; j < rooms.Count; j++)
                    {
                        var existing = rooms[j];
                        Assert.That(!existing.Intersects(bounds), $"Found overlapping rooms {i} and {j} in DungeonRoomPack {pack.ID}");
                    }

                    rooms.Add(bounds);

                    // Inclusive of upper bounds as it's the edge
                    Assert.That(room.Left >= 0 &&
                                room.Bottom >= 0 &&
                                room.Right <= pack.Size.X &&
                                room.Top <= pack.Size.Y, $"Found invalid room {room} on DungeonRoomPack {pack.ID}");

                    // Assert that anything exists at this size
                    var rotated = new Vector2i(room.Size.Y, room.Size.X);

                    Assert.That(sizes.Contains(room.Size) || sizes.Contains(rotated), $"Didn't find any dungeon room prototypes for {room.Size} on {pack.ID} index {i}");
                }
            }
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestDungeonPresets()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoManager = pair.Server.ResolveDependency<IPrototypeManager>();

        await pair.Server.WaitAssertion(() =>
        {
            var sizes = new HashSet<Vector2i>();

            foreach (var pack in protoManager.EnumeratePrototypes<DungeonRoomPackPrototype>())
            {
                sizes.Add(pack.Size);
                sizes.Add(new Vector2i(pack.Size.Y, pack.Size.X));
            }

            foreach (var preset in protoManager.EnumeratePrototypes<DungeonPresetPrototype>())
            {
                for (var i = 0; i < preset.RoomPacks.Count; i++)
                {
                    var pack = preset.RoomPacks[i];

                    // Assert that anything exists at this size
                    var rotated = new Vector2i(pack.Size.Y, pack.Size.X);

                    Assert.Multiple(() =>
                    {
                        Assert.That(sizes.Contains(pack.Size) || sizes.Contains(rotated), $"Didn't find any dungeon room prototypes for {pack.Size} for {preset.ID} index {i}");
                        Assert.That(pack.Bottom, Is.GreaterThanOrEqualTo(0), "All dungeon room packs need their y-axis to be above 0!");
                    });
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
