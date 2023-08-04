using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Server.Preferences
{
    [TestFixture]
    public sealed class ServerDbSqliteTests : ContentUnitTest
    {
        private const string Prototypes = @"
- type: dataset
  id: names_first_male
  values:
  - Aaden

- type: dataset
  id: names_first_female
  values:
  - Aaliyah

- type: dataset
  id: names_last
  values:
  - Ackerley";

        private static HumanoidCharacterProfile CharlieCharlieson()
        {
            return new(
                "Charlie Charlieson",
                "The biggest boy around.",
                "Human",
                21,
                Sex.Male,
                Gender.Epicene,
                new HumanoidCharacterAppearance(
                    "Afro",
                    Color.Aqua,
                    "Shaved",
                    Color.Aquamarine,
                    Color.Azure,
                    Color.Beige,
                    new ()
                ),
                ClothingPreference.Jumpskirt,
                BackpackPreference.Backpack,
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.FallbackOverflowJob, JobPriority.High}
                },
                PreferenceUnavailableMode.StayInLobby,
                new List<string> (),
                new List<string>()
            );
        }

        private static ServerDbSqlite GetDb()
        {
            var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            builder.UseSqlite(conn);
            return new ServerDbSqlite(() => builder.Options, true);
        }

        [Test]
        public async Task TestUserDoesNotExist()
        {
            var db = GetDb();
            // Database should be empty so a new GUID should do it.
            Assert.Null(await db.GetPlayerPreferencesAsync(NewUserId()));
        }

        [Test]
        public async Task TestInitPrefs()
        {
            var db = GetDb();
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
            var db = GetDb();
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            prototypeManager.LoadFromStream(new StringReader(Prototypes));
            await db.InitPrefsAsync(username, new HumanoidCharacterProfile());
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), 1);
            await db.SaveSelectedCharacterIndexAsync(username, 1);
            await db.SaveCharacterSlotAsync(username, null, 1);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(!prefs.Characters.Any(p => p.Key != 0));
        }

        private static NetUserId NewUserId()
        {
            return new(Guid.NewGuid());
        }
    }
}
