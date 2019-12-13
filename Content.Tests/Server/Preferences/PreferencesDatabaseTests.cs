using System.IO;
using Content.Server.Preferences;
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

        private static ICharacterProfile CharlieCharlieson()
        {
            return new HumanoidCharacterProfile
            {
                Name = "Charlie Charlieson",
                Age = 21,
                Sex = Sex.Male,
                CharacterAppearance = new HumanoidCharacterAppearance()
                {
                    HairStyleName = "Afro",
                    HairColor = Color.Aqua,
                    FacialHairStyleName = "Shaved",
                    FacialHairColor = Color.Aquamarine,
                    EyeColor = Color.Azure,
                    SkinColor = Color.Beige
                }
            };
        }

        private static PreferencesDatabase GetDb()
        {
            return new PreferencesDatabase(Path.GetTempFileName(), MaxCharacterSlots);
        }

        [Test]
        public void TestUserDoesNotExist()
        {
            var db = GetDb();
            Assert.Null(db.GetPlayerPreferences("[The database should be empty so any string should do]"));
        }

        [Test]
        public void TestUserDoesExist()
        {
            var db = GetDb();
            const string username = "bobby";
            db.SaveSelectedCharacterIndex(username, 0);
            var prefs = db.GetPlayerPreferences(username);
            Assert.NotNull(prefs);
            Assert.Zero(prefs.SelectedCharacterIndex);
            Assert.That(prefs.Characters.TrueForAll(character => character is null));
        }

        [Test]
        public void TestUpdateCharacter()
        {
            var db = GetDb();
            const string username = "charlie";
            const int slot = 0;
            var originalProfile = CharlieCharlieson();
            db.SaveSelectedCharacterIndex(username, slot);
            db.SaveCharacterSlot(username, originalProfile, slot);
            var prefs = db.GetPlayerPreferences(username);
            Assert.That(prefs.Characters[slot].MemberwiseEquals(originalProfile));
        }

        [Test]
        public void TestDeleteCharacter()
        {
            var db = GetDb();
            const string username = "charlie";
            const int slot = 0;
            db.SaveSelectedCharacterIndex(username, slot);
            db.SaveCharacterSlot(username, CharlieCharlieson(), slot);
            db.SaveCharacterSlot(username, null, slot);
            var prefs = db.GetPlayerPreferences(username);
            Assert.That(prefs.Characters.TrueForAll(character => character is null));
        }

        [Test]
        public void TestInvalidSlot()
        {
            var db = GetDb();
            const string username = "charlie";
            const int slot = -1;

            db.SaveSelectedCharacterIndex(username, slot);
            db.SaveCharacterSlot(username, CharlieCharlieson(), slot);
            var prefs = db.GetPlayerPreferences(username);
            Assert.AreEqual(prefs.SelectedCharacterIndex, 0);

            db.SaveSelectedCharacterIndex(username, MaxCharacterSlots);
            prefs = db.GetPlayerPreferences(username);
            Assert.AreEqual(prefs.SelectedCharacterIndex, MaxCharacterSlots-1);
        }
    }
}
