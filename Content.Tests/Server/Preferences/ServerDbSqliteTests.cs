using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Database.Entity;
using Content.Shared;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.UnitTesting;
using Robust.Shared.IoC;

namespace Content.Tests.Server.Preferences
{
    [TestFixture]
    public class ServerDbSqliteTests : ContentUnitTest
    {
        private static HumanoidCharacterProfile CharlieCharlieson()
        {
            return new(
                "Charlie Charlieson",
                21,
                Sex.Male,
                new HumanoidCharacterAppearance(
                    "Afro",
                    Color.Aqua,
                    "Shaved",
                    Color.Aquamarine,
                    Color.Azure,
                    Color.Beige
                ),
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.OverflowJob, JobPriority.High}
                },
                PreferenceUnavailableMode.StayInLobby,
                new List<string> ()
            );
        }

        private static IServerDbManager GetAndInitDb()
        {
            IServerDbManager isdm = IoCManager.Resolve<IServerDbManager>();
            isdm.Init();
            return isdm;
        }

        [Test]
        public async Task TestUserDoesNotExist()
        {
            var db = GetAndInitDb();
            // Database should be empty so a new GUID should do it.
            Assert.Null(await db.GetPlayerPreferencesAsync(NewUserId()));
        }

        [Test]
        public async Task TestInitPrefs()
        {
            var db = GetAndInitDb();
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            const int slot = 0;
            var originalProfile = CharlieCharlieson();
            await db.InitPrefsAsync(username, originalProfile);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.Single(p => p.Key == slot).Value.MemberwiseEquals(originalProfile));
        }

        [Test]
        public async Task TestDeleteCharacter()
        {
            var db = GetAndInitDb();
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            await db.InitPrefsAsync(username, HumanoidCharacterProfile.Default());
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), 1);
            await db.SaveSelectedCharacterIndexAsync(username, 1);
            await db.SaveCharacterSlotAsync(username, null, 1);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(!prefs.Characters.Any(p => p.Key != 0));
        }

        /// <summary>
        /// The API, or database interference, allows deleting all character slots.
        /// In particular this could happen if someone did a global wipe of all profiles.
        /// In this case, it would be nice if the game recovered.
        /// </summary>
        [Test]
        public async Task TestDeliberatelyBreakConsistencyAndRecover()
        {
            var db = GetAndInitDb();
            var username = new NetUserId(new Guid("bad21c00-1929-4100-b57c-e6352237ce05"));
            // Initialize account
            await db.InitPrefsAsync(username, HumanoidCharacterProfile.Default());
            // Oh no, they somehow got the server to delete their only slot
            await db.SaveCharacterSlotAsync(username, null, 0);
            // Database returns null to get content to reinitialize
            Assert.That(await db.GetPlayerPreferencesAsync(username) == null);
            // Requested reinitialize happens
            await db.InitPrefsAsync(username, HumanoidCharacterProfile.Default());
            // Consistency fixed
            Assert.That(await db.GetPlayerPreferencesAsync(username) != null);
        }

        private static NetUserId NewUserId()
        {
            return new(Guid.NewGuid());
        }
    }
}
