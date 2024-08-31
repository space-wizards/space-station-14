using System.Collections.Generic;
using System.Linq;
using Content.Shared.Announcements.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Announcers;

/// <summary>
///     Checks if every announcer has a fallback announcement
/// </summary>
[TestFixture]
[TestOf(typeof(AnnouncerPrototype))]
public sealed class AnnouncerPrototypeTest
{
    /// <inheritdoc cref="AnnouncerPrototypeTest"/>
    [Test]
    public async Task TestAnnouncerFallbacks()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var prototype = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var success = true;
            var why = new List<string>();

            foreach (var announcer in prototype.EnumeratePrototypes<AnnouncerPrototype>().OrderBy(a => a.ID))
            {
                if (announcer.Announcements.Any(a => a.ID.ToLower() == "fallback"))
                    continue;

                success = false;
                why.Add(announcer.ID);
            }

            Assert.That(success, Is.True, $"The following announcers do not have a fallback announcement:\n  {string.Join("\n  ", why)}");
        });

        await pair.CleanReturnAsync();
    }
}
