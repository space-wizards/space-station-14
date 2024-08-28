using System.Collections.Generic;
using System.Linq;
using Content.Server.Announcements.Systems;
using Content.Server.StationEvents;
using Content.Shared.Announcements.Prototypes;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.IntegrationTests.Tests.Announcers;

/// <summary>
///     Checks if every station event using the announcerSystem has a valid audio file associated with it
/// </summary>
[TestFixture]
[TestOf(typeof(AnnouncerPrototype))]
public sealed class AnnouncerAudioTest
{
    /// <inheritdoc cref="AnnouncerAudioTest" />
    [Test]
    public async Task TestEventSounds()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var client = pair.Client;

        var entSysMan = server.ResolveDependency<IEntitySystemManager>();
        var proto = server.ResolveDependency<IPrototypeManager>();
        var cache = client.ResolveDependency<IResourceCache>();
        var announcer = entSysMan.GetEntitySystem<AnnouncerSystem>();
        var events = entSysMan.GetEntitySystem<EventManagerSystem>();

        await server.WaitAssertion(() =>
        {
            var succeeded = true;
            var why = new List<string>();

            foreach (var announcerProto in proto.EnumeratePrototypes<AnnouncerPrototype>().OrderBy(a => a.ID))
            {
                foreach (var ev in events.AllEvents())
                {
                    if (ev.Value.StartAnnouncement)
                    {
                        var announcementId = announcer.GetAnnouncementId(ev.Key.ID);
                        var path = announcer.GetAnnouncementPath(announcementId, announcerProto);

                        if (!cache.ContentFileExists(path))
                        {
                            succeeded = false;
                            why.Add($"\"{announcerProto.ID}\", \"{announcementId}\": \"{path}\"");
                        }
                    }

                    if (ev.Value.EndAnnouncement)
                    {
                        var announcementId = announcer.GetAnnouncementId(ev.Key.ID, true);
                        var path = announcer.GetAnnouncementPath(announcementId, announcerProto);

                        if (!cache.ContentFileExists(path))
                        {
                            succeeded = false;
                            why.Add($"\"{announcerProto.ID}\", \"{announcementId}\": \"{path}\"");
                        }
                    }
                }
            }

            Assert.That(succeeded, Is.True, $"The following announcements do not have a valid announcement audio:\n  {string.Join("\n  ", why)}");
        });

        await pair.CleanReturnAsync();
    }
}
