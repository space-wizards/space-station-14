using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    // This code is responsible for the assigning & picking of jobs.
    public partial class GameTicker
    {
        [ViewVariables]
        private readonly Dictionary<string, int> _spawnedPositions = new Dictionary<string, int>();

        private Dictionary<IPlayerSession, string> AssignJobs(List<IPlayerSession> available,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
        {
            // Calculate positions available round-start for each job.
            var availablePositions = GetBasePositions(true);

            // Output dictionary of assigned jobs.
            var assigned = new Dictionary<IPlayerSession, string>();

            // Go over each priority level top to bottom.
            for (var i = JobPriority.High; i > JobPriority.Never; i--)
            {
                void ProcessJobs(bool heads)
                {
                    // Get all candidates for this priority & heads combo.
                    // That is all people with at LEAST one job at this priority & heads level,
                    // and the jobs they have selected here.
                    var candidates = available
                        .Select(player =>
                        {
                            var profile = profiles[player.UserId];

                            var availableJobs = profile.JobPriorities
                                .Where(j =>
                                {
                                    var (jobId, priority) = j;
                                    var job = _prototypeManager.Index<JobPrototype>(jobId);
                                    if (job.IsHead != heads)
                                    {
                                        return false;
                                    }

                                    return priority == i;
                                })
                                .Select(j => j.Key)
                                .ToList();

                            return (player, availableJobs);
                        })
                        .Where(p => p.availableJobs.Count != 0)
                        .ToList();

                    _robustRandom.Shuffle(candidates);

                    foreach (var (candidate, jobs) in candidates)
                    {
                        while (jobs.Count != 0)
                        {
                            var picked = _robustRandom.Pick(jobs);

                            var openPositions = availablePositions.GetValueOrDefault(picked, 0);
                            if (openPositions == 0)
                            {
                                jobs.Remove(picked);
                                continue;
                            }

                            availablePositions[picked] -= 1;
                            assigned.Add(candidate, picked);
                            break;
                        }
                    }

                    available.RemoveAll(a => assigned.ContainsKey(a));
                }

                // Process heads FIRST.
                // This means that if you have head and non-head roles on the same priority level,
                // you will always get picked as head.
                // Unless of course somebody beats you to those head roles.
                ProcessJobs(true);
                ProcessJobs(false);
            }

            return assigned;
        }

        /// <summary>
        ///     Gets the available positions for all jobs, *not* accounting for the current crew manifest.
        /// </summary>
        private Dictionary<string, int> GetBasePositions(bool roundStart)
        {
            var availablePositions = _prototypeManager
                .EnumeratePrototypes<JobPrototype>()
                // -1 is treated as infinite slots.
                .ToDictionary(job => job.ID, job =>
                {
                    if (job.SpawnPositions < 0)
                    {
                        return int.MaxValue;
                    }

                    if (roundStart)
                    {
                        return job.SpawnPositions;
                    }

                    return job.TotalPositions;
                });

            return availablePositions;
        }

        /// <summary>
        ///     Gets the remaining available job positions in the current round.
        /// </summary>
        public Dictionary<string, int> GetAvailablePositions()
        {
            var basePositions = GetBasePositions(false);

            foreach (var (jobId, count) in _spawnedPositions)
            {
                basePositions[jobId] = Math.Max(0, basePositions[jobId] - count);
            }

            return basePositions;
        }

        private string PickBestAvailableJob(HumanoidCharacterProfile profile)
        {
            var available = GetAvailablePositions();

            bool TryPick(JobPriority priority, out string jobId)
            {
                var filtered = profile.JobPriorities
                    .Where(p => p.Value == priority)
                    .Select(p => p.Key)
                    .ToList();

                while (filtered.Count != 0)
                {
                    jobId = _robustRandom.Pick(filtered);
                    if (available.GetValueOrDefault(jobId, 0) > 0)
                    {
                        return true;
                    }

                    filtered.Remove(jobId);
                }

                jobId = default;
                return false;
            }

            if (TryPick(JobPriority.High, out var picked))
            {
                return picked;
            }

            if (TryPick(JobPriority.Medium, out picked))
            {
                return picked;
            }

            if (TryPick(JobPriority.Low, out picked))
            {
                return picked;
            }

            return OverflowJob;
        }

        [Conditional("DEBUG")]
        private void JobControllerInit()
        {
            // Verify that the overflow role exists and has the correct name.
            var role = _prototypeManager.Index<JobPrototype>(OverflowJob);
            DebugTools.Assert(role.Name == Loc.GetString(OverflowJobName),
                "Overflow role does not have the correct name!");

            DebugTools.Assert(role.SpawnPositions < 0, "Overflow role must have infinite spawn positions!");
        }

        private void AddSpawnedPosition(string jobId)
        {
            _spawnedPositions[jobId] = _spawnedPositions.GetValueOrDefault(jobId, 0) + 1;
        }
    }
}
