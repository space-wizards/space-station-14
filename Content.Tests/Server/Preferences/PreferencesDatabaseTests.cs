using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Preferences;
using Content.Shared;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.UnitTesting;

namespace Content.Tests.Server.Preferences
{
    [TestFixture]
    public class PreferencesDatabaseTests : RobustUnitTest
    {
        private const int MaxCharacterSlots = 10;

        private static HumanoidCharacterProfile CharlieCharlieson()
        {
            return new HumanoidCharacterProfile(
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
                new List<string>{}
            );
        }

        private static PreferencesDatabase GetDb()
        {
            return new PreferencesDatabase(new SqliteConfiguration(Path.GetTempFileName()), MaxCharacterSlots);
        }

        [Test]
        public async Task TestUserDoesNotExist()
        {
            var db = GetDb();
            Assert.Null(await db.GetPlayerPreferencesAsync("[The database should be empty so any string should do]"));
        }

        [Test]
        public async Task TestUserDoesExist()
        {
            var db = GetDb();
            const string username = "bobby";
            await db.SaveSelectedCharacterIndexAsync(username, 0);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.NotNull(prefs);
            Assert.Zero(prefs.SelectedCharacterIndex);
            Assert.That(prefs.Characters.ToList().TrueForAll(character => character is null));
        }

        [Test]
        public async Task TestUpdateCharacter()
        {
            var db = GetDb();
            const string username = "charlie";
            const int slot = 0;
            var originalProfile = CharlieCharlieson();
            await db.SaveSelectedCharacterIndexAsync(username, slot);
            await db.SaveCharacterSlotAsync(username, originalProfile, slot);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.ElementAt(slot).MemberwiseEquals(originalProfile));
        }

        [Test]
        public async Task TestDeleteCharacter()
        {
            var db = GetDb();
            const string username = "charlie";
            const int slot = 0;
            await db.SaveSelectedCharacterIndexAsync(username, slot);
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), slot);
            await db.SaveCharacterSlotAsync(username, null, slot);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.ToList().TrueForAll(character => character is null));
        }

        [Test]
        public async Task TestInvalidSlot()
        {
            var db = GetDb();
            const string username = "charlie";
            const int slot = -1;

            await db.SaveSelectedCharacterIndexAsync(username, slot);
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), slot);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.AreEqual(prefs.SelectedCharacterIndex, 0);

            await db.SaveSelectedCharacterIndexAsync(username, MaxCharacterSlots);
            prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.AreEqual(prefs.SelectedCharacterIndex, MaxCharacterSlots - 1);
        }
    }
}
