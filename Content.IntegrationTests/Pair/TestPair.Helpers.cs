#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    public Task<TestMapData> CreateTestMap(bool initialized = true)
        => CreateTestMap(initialized, "Plating");

    /// <summary>
    /// Set a user's antag preferences. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetAntagPreference(ProtoId<AntagPrototype> id, bool value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is not {} userId)
            return;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = (HumanoidCharacterProfile) prefs.Characters[0];
        var newProfile = profile.WithAntagPreference(id, value);
        _modifiedProfiles.Add(userId);
        await Server.WaitPost(() => prefMan.SetProfile(userId, 0, newProfile).Wait());
    }

    /// <summary>
    /// Set a user's job preferences.  Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetJobPriority(ProtoId<JobPrototype> id, JobPriority value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is { } userId)
            await SetJobPriorities(userId, (id, value));
    }

    /// <inheritdoc cref="SetJobPriority"/>
    public async Task SetJobPriorities(params (ProtoId<JobPrototype>, JobPriority)[] priorities)
        => await SetJobPriorities(Client.User!.Value, priorities);

    /// <inheritdoc cref="SetJobPriority"/>
    public async Task SetJobPriorities(NetUserId user, params (ProtoId<JobPrototype>, JobPriority)[] priorities)
    {
        var highCount = priorities.Count(x => x.Item2 == JobPriority.High);
        Assert.That(highCount, Is.LessThanOrEqualTo(1), "Cannot have more than one high priority job");

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(user);
        var profile = (HumanoidCharacterProfile) prefs.Characters[0];
        var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(profile.JobPriorities);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        if (highCount != 0)
        {
            foreach (var (key, priority) in dictionary)
            {
                if (priority == JobPriority.High)
                    dictionary[key] = JobPriority.Medium;
            }
        }

        foreach (var (job, priority) in priorities)
        {
            if (priority == JobPriority.Never)
                dictionary.Remove(job);
            else
                dictionary[job] = priority;
        }

        var newProfile = profile.WithJobPriorities(dictionary);
        _modifiedProfiles.Add(user);
        await Server.WaitPost(() => prefMan.SetProfile(user, 0, newProfile).Wait());
    }
}
