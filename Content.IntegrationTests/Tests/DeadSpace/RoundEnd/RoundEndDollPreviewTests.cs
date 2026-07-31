// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Content.Client.DeadSpace.RoundEnd;
using Content.Shared.DeadSpace.RoundEnd;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.DeadSpace.RoundEnd;

[TestFixture]
[NonParallelizable]
public sealed class RoundEndDollPreviewTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: RoundEndDollTestNoSprite
  name: round end doll test entity without a sprite
";

    [Test]
    public async Task QueueBuildsCachesRebuildsAfterRoundCleanupAndCleansUp()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var previews = client.System<RoundEndDollPreviewSystem>();
            var owner = previews.CreateOwner();
            var originalEntityCount = client.EntMan.EntityCount;
            var callbacks = 0;
            var snapshots = new List<EntityUid>();

            var humanoid = new RoundEndDollData
            {
                BodyPrototype = "MobHuman",
                Equipment =
                [
                    new RoundEndDollEquipment
                    {
                        Slot = "jumpsuit",
                        Prototype = "RoundEndDollMissingEquipmentPrototype",
                    },
                ],
            };
            var brokenBody = new RoundEndDollData
            {
                BodyPrototype = "RoundEndDollTestNoSprite",
            };

            previews.Request(owner, humanoid, snapshot =>
            {
                callbacks++;
                Assert.That(snapshot, Is.Not.Null);
                snapshots.Add(snapshot!.Value);
            });
            previews.Request(owner, brokenBody, snapshot =>
            {
                callbacks++;
                Assert.That(snapshot, Is.Not.Null);
                snapshots.Add(snapshot!.Value);
            });

            previews.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(snapshots, Has.Count.EqualTo(1));
                Assert.That(client.EntMan.HasComponent<SpriteComponent>(snapshots[0]), Is.True);
            });

            previews.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(callbacks, Is.EqualTo(2));
                Assert.That(snapshots, Has.Count.EqualTo(2));
                Assert.That(client.EntMan.HasComponent<SpriteComponent>(snapshots[1]), Is.True);
                Assert.That(client.EntMan.EntityCount, Is.EqualTo(originalEntityCount + 2));
            });

            EntityUid? cached = null;
            previews.Request(owner, humanoid, snapshot => cached = snapshot);
            Assert.That(cached, Is.EqualTo(snapshots[0]));
            Assert.That(client.EntMan.EntityCount, Is.EqualTo(originalEntityCount + 2));

            client.EntMan.EventBus.RaiseEvent(EventSource.Network, new RoundRestartCleanupEvent());
            Assert.That(snapshots.Take(2).All(client.EntMan.Deleted), Is.True);
            var entityCountAfterCleanup = client.EntMan.EntityCount;

            // The first update waits for the rest of round cleanup, then one snapshot is rebuilt per update.
            previews.Update(0f);
            Assert.That(callbacks, Is.EqualTo(2));
            previews.Update(0f);
            Assert.That(callbacks, Is.EqualTo(3));
            previews.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(callbacks, Is.EqualTo(4));
                Assert.That(snapshots, Has.Count.EqualTo(4));
                Assert.That(snapshots.Skip(2).All(snapshot => !client.EntMan.Deleted(snapshot)), Is.True);
                Assert.That(cached, Is.Not.Null);
                Assert.That(snapshots.Skip(2), Does.Contain(cached!.Value));
                Assert.That(client.EntMan.EntityCount, Is.EqualTo(entityCountAfterCleanup + 2));
            });

            previews.Request(owner, new RoundEndDollData { BodyPrototype = "MobHuman" }, _ => callbacks++);
            previews.Cancel(owner);
            previews.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(callbacks, Is.EqualTo(4));
                Assert.That(snapshots.All(client.EntMan.Deleted), Is.True);
                Assert.That(client.EntMan.EntityCount, Is.EqualTo(entityCountAfterCleanup));
            });
        });

        await pair.CleanReturnAsync();
    }
}
