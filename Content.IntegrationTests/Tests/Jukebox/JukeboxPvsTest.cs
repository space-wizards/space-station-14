using Content.Server.Audio.Jukebox;
using Content.Shared.Audio.Jukebox;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Jukebox;

public sealed class JukeboxPvsTest
{
    private static readonly EntProtoId JukeboxProtoId = "Jukebox";

    private const string TrackId = "JukeboxTest";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: jukebox
  id: {TrackId}
  name: Test
  path:
    path: /Audio/Effects/beep1.ogg
";

    /// <summary>
    /// Tests that a jukebox has a valid PVS state after a track finishes playing.
    /// </summary>
    [Test]
    public async Task TrackEndTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var jukeboxSys = server.System<JukeboxSystem>();

        var mapData = await pair.CreateTestMap();

        EntityUid jukebox = default;
        await server.WaitAssertion(() =>
        {
            // Spawn a jukebox
            jukebox = entMan.SpawnEntity(JukeboxProtoId, mapData.MapCoords);

            // Get the prototype for our test track
            var track = protoMan.Index<JukeboxPrototype>(TrackId);

            // Select the test track
            jukeboxSys.SetSelectedTrack(jukebox, track);
            // Start playing
            jukeboxSys.TryPlay(jukebox);

            // Make sure it's playing
            Assert.That(jukeboxSys.IsPlaying(jukebox));
        });

        // Wait a moment for the music to end
        await pair.RunSeconds(5);

        await server.WaitAssertion(() =>
        {
            // Make sure the music has stopped
            Assert.That(jukeboxSys.IsPlaying(jukebox), Is.False);
        });

        // Add a fake player session
        var dummySession = await server.AddDummySession();

        // Force a PVS update for the fake player
        await server.WaitAssertion(() =>
        {
            Assert.DoesNotThrow(() => server.PvsTick([dummySession]));
        });

        await server.WaitRunTicks(5);
        await pair.CleanReturnAsync();
    }
}
