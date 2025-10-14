#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    public Task<TestMapData> CreateTestMap(bool initialized = true)
        => CreateTestMap(initialized, "Plating");

    /// <summary>
    /// Add dummy players to the pair with server saved job priority preferences
    /// </summary>
    /// <param name="jobPriorities">Job priorities to initialize the players with</param>
    /// <param name="count">How many players to add</param>
    /// <returns>Enumerable of sessions for the new players</returns>
    [PublicAPI]
    public async Task<IEnumerable<ICommonSession>> AddDummyPlayers(
        Dictionary<ProtoId<JobPrototype>,JobPriority> jobPriorities,
        int count=1)
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var dbMan = Server.ResolveDependency<UserDbDataManager>();

        var sessions = await Server.AddDummySessions(count);
        await RunTicksSync(5);
        var tasks = sessions.Select(s =>
        {
            // dbMan.ClientConnected(s);
            dbMan.WaitLoadComplete(s).Wait();
            var newProfile = HumanoidCharacterProfile.Random().WithJobPriorities(jobPriorities);
            return prefMan.SetProfile(s.UserId, 0, newProfile);
        });
        await Server.WaitPost(() => Task.WhenAll(tasks).Wait());
        await RunTicksSync(5);

        return sessions;
    }
}
