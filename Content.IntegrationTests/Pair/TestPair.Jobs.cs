#nullable enable

using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    /// <summary>
    /// Set the job priorities for a session
    /// </summary>
    /// <param name="player">session to modify</param>
    /// <param name="jobPriorities">job priorities to set</param>
    public async Task SetJobPriorities(ICommonSession player,
        Dictionary<ProtoId<JobPrototype>,JobPriority> jobPriorities)
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            prefMan.SetJobPriorities(player.UserId, jobPriorities).Wait();
        });
    }

    /// <summary>
    /// Set the job priorities for the TestPair.Player session
    /// </summary>
    /// <param name="jobPriorities">job priorities to set</param>
    public Task SetJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
    {
        return SetJobPriorities(Player!, jobPriorities);
    }

    /// <summary>
    /// Set the job preferences for the TestPair.Player session, specifically the character in slot 0
    /// </summary>
    /// <param name="jobPreferences">job preferences to set</param>
    public async Task SetJobPreferences(HashSet<ProtoId<JobPrototype>> jobPreferences)
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            var profile = prefMan.GetPreferences(Player!.UserId).Characters[0] as HumanoidCharacterProfile;
            prefMan.SetProfile(Player!.UserId, 0, profile!.WithJobPreferences(jobPreferences)).Wait();
        });
    }

    /// <summary>
    /// Set the antag preferences for a session, specifically the character in slot 0
    /// </summary>
    /// <param name="player">session to modify</param>
    /// <param name="antagPreferences">antag preferences to set</param>
    public async Task SetAntagPreferences(ICommonSession player, HashSet<ProtoId<AntagPrototype>> antagPreferences)
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            var profile = prefMan.GetPreferences(player.UserId).Characters[0] as HumanoidCharacterProfile;
            prefMan.SetProfile(player.UserId, 0, profile!.WithAntagPreferences(antagPreferences)).Wait();
        });
    }

    /// <summary>
    /// Set the antag preferences for the TestPair.Player session, specifically the character in slot 0
    /// </summary>
    /// <param name="antagPreferences">antag preferences to set</param>
    public Task SetAntagPreferences(HashSet<ProtoId<AntagPrototype>> antagPreferences)
    {
        return SetAntagPreferences(Player!, antagPreferences);
    }

    public void AssertJob(
        ProtoId<JobPrototype> job,
        ICommonSession session,
        bool isAntag = false)
    {
        AssertJob(job, session.UserId, isAntag);
    }

    public void AssertJob(ProtoId<JobPrototype> job, NetUserId? user = null, bool isAntag = false)
    {
        var jobSys = Server.System<SharedJobSystem>();
        var mindSys = Server.System<SharedMindSystem>();
        var roleSys = Server.System<SharedRoleSystem>();
        var ticker = Server.System<GameTicker>();

        user ??= Client.User!.Value;

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses[user.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));

        var uid = Server.PlayerMan.SessionsDict.GetValueOrDefault(user.Value)?.AttachedEntity;
        Assert.That(Server.EntMan.EntityExists(uid));
        var mind = mindSys.GetMind(uid!.Value);
        Assert.That(Server.EntMan.EntityExists(mind));
        Assert.That(jobSys.MindTryGetJobId(mind, out var actualJob));
        Assert.That(actualJob, Is.EqualTo(job));
        Assert.That(roleSys.MindIsAntagonist(mind), Is.EqualTo(isAntag));
    }
}
