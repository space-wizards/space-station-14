using System;
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
            // Database should be empty so a new GUID should do it.
            Assert.Null(await db.GetPlayerPreferencesAsync(Guid.NewGuid()));
        }

        [Test]
        public async Task TestUserDoesExist()
        {
            var db = GetDb();
            var username = new Guid("9efe231c-47a6-40f3-9dc2-317e9571de6e");
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
            var username = new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd");
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
            var username = new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd");
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
            var username = new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd");
            const int slot = -1;

            await db.SaveSelectedCharacterIndexAsync(username, slot);
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), slot);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

            await db.SaveSelectedCharacterIndexAsync(username, MaxCharacterSlots);
            prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(MaxCharacterSlots - 1));
        }
    }
}
