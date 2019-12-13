using System;
using System.Linq;
using Content.Server.Preferences.Migrations;
using Content.Shared.Preferences;
using Dapper;
using Microsoft.Data.Sqlite;
using Robust.Shared.Maths;
using static Content.Shared.Preferences.Sex;

namespace Content.Server.Preferences
{
    /// <summary>
    /// Provides methods to retrieve and update character preferences.
    /// Don't use this directly, go through <see cref="ServerPreferencesManager"/> instead.
    /// </summary>
    public class PreferencesDatabase
    {
        private readonly string _databaseFilePath;
        private readonly int _maxCharacterSlots;

        public PreferencesDatabase(string databaseFilePath, int maxCharacterSlots)
        {
            _databaseFilePath = databaseFilePath;
            _maxCharacterSlots = maxCharacterSlots;
            MigrationManager.PerformUpgrade(GetDbConnectionString());
        }

        private string GetDbConnectionString()
        {
            return new SqliteConnectionStringBuilder
            {
                DataSource = _databaseFilePath,
            }.ToString();
        }

        private SqliteConnection GetDbConnection()
        {
            var connectionString = GetDbConnectionString();
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }

        private const string PlayerPreferencesQuery =
            @"SELECT Id, SelectedCharacterIndex FROM PlayerPreferences WHERE Username=@Username";

        private const string HumanoidCharactersQuery =
            @"SELECT Slot, Name, Age, Sex, HairStyleName, HairColor, FacialHairStyleName, FacialHairColor, EyeColor, SkinColor
              FROM HumanoidCharacterProfiles
              WHERE Player = @Id";

        private sealed class PlayerPreferencesSql
        {
            public int Id { get; set; }
            public int SelectedCharacterIndex { get; set; }
        }

        public PlayerPreferences GetPlayerPreferences(string username)
        {
            using (var connection = GetDbConnection())
            {
                var prefs = connection.QueryFirstOrDefault<PlayerPreferencesSql>(
                    PlayerPreferencesQuery,
                    new {Username = username});
                if (prefs is null)
                {
                    return null;
                }

                // Using Dapper for ICharacterProfile and ICharacterAppearance is annoying so
                // we do it manually
                var cmd = new SqliteCommand(HumanoidCharactersQuery, connection);
                cmd.Parameters.AddWithValue("@Id", prefs.Id);
                cmd.Prepare();

                var reader = cmd.ExecuteReader();
                var profiles = new ICharacterProfile[_maxCharacterSlots];
                while (reader.Read())
                {
                    profiles[reader.GetInt32(0)] = new HumanoidCharacterProfile
                    {
                        Name = reader.GetString(1),
                        Age = reader.GetInt32(2),
                        Sex = reader.GetString(3) == "Male" ? Male : Female,
                        CharacterAppearance = new HumanoidCharacterAppearance
                        {
                            HairStyleName = reader.GetString(4),
                            HairColor = Color.FromHex(reader.GetString(5)),
                            FacialHairStyleName = reader.GetString(6),
                            FacialHairColor = Color.FromHex(reader.GetString(7)),
                            EyeColor = Color.FromHex(reader.GetString(8)),
                            SkinColor = Color.FromHex(reader.GetString(9))
                        }
                    };
                }

                return new PlayerPreferences
                {
                    SelectedCharacterIndex = prefs.SelectedCharacterIndex,
                    Characters = profiles.ToList()
                };
            }
        }

        private const string SaveSelectedCharacterIndexQuery =
            @"UPDATE PlayerPreferences
              SET SelectedCharacterIndex = @SelectedCharacterIndex
              WHERE Username = @Username;

              -- If no update happened (i.e. the row didn't exist) then insert one // https://stackoverflow.com/a/38463024
              INSERT INTO PlayerPreferences
              (SelectedCharacterIndex, Username)
              SELECT
              @SelectedCharacterIndex,
              @Username
              WHERE (SELECT Changes() = 0);";

        public void SaveSelectedCharacterIndex(string username, int index)
        {
            index = index.Clamp(0, _maxCharacterSlots - 1);
            using (var connection = GetDbConnection())
            {
                connection.Execute(SaveSelectedCharacterIndexQuery,
                    new {SelectedCharacterIndex = index, Username = username});
            }
        }

        private const string SaveCharacterSlotQuery =
            @"UPDATE HumanoidCharacterProfiles
              SET
              Name = @Name,
              Age = @Age,
              Sex = @Sex,
              HairStyleName = @HairStyleName,
              HairColor = @HairColor,
              FacialHairStyleName = @FacialHairStyleName,
              FacialHairColor = @FacialHairColor,
              EyeColor = @EyeColor,
              SkinColor = @SkinColor
              WHERE Slot = @Slot AND Player = (SELECT Id FROM PlayerPreferences WHERE Username = @Username);

              -- If no update happened (i.e. the row didn't exist) then insert one // https://stackoverflow.com/a/38463024
              INSERT INTO HumanoidCharacterProfiles
              (Slot,
              Player,
              Name,
              Age,
              Sex,
              HairStyleName,
              HairColor,
              FacialHairStyleName,
              FacialHairColor,
              EyeColor,
              SkinColor)
              SELECT
              @Slot,
              (SELECT Id FROM PlayerPreferences WHERE Username = @Username),
              @Name,
              @Age,
              @Sex,
              @HairStyleName,
              @HairColor,
              @FacialHairStyleName,
              @FacialHairColor,
              @EyeColor,
              @SkinColor
              WHERE (SELECT Changes() = 0);";

        public void SaveCharacterSlot(string username, ICharacterProfile profile, int slot)
        {
            if (slot < 0 || slot >= _maxCharacterSlots)
                return;
            if (profile is null)
            {
                DeleteCharacterSlot(username, slot);
                return;
            }

            if (!(profile is HumanoidCharacterProfile humanoid))
            {
                // TODO: Handle other ICharacterProfile implementations properly
                throw new NotImplementedException();
            }

            var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
            using (var connection = GetDbConnection())
            {
                connection.Execute(SaveCharacterSlotQuery, new
                {
                    Name = humanoid.Name,
                    Age = humanoid.Age,
                    Sex = humanoid.Sex.ToString(),
                    HairStyleName = appearance.HairStyleName,
                    HairColor = appearance.HairColor.ToHex(),
                    FacialHairStyleName = appearance.FacialHairStyleName,
                    FacialHairColor = appearance.FacialHairColor.ToHex(),
                    EyeColor = appearance.EyeColor.ToHex(),
                    SkinColor = appearance.SkinColor.ToHex(),
                    Slot = slot,
                    Username = username
                });
            }
        }

        private const string DeleteCharacterSlotQuery =
            @"DELETE FROM HumanoidCharacterProfiles
              WHERE
              Player = (SELECT Id FROM PlayerPreferences WHERE Username = @Username)
              AND
              Slot = @Slot";

        private void DeleteCharacterSlot(string username, int slot)
        {
            using (var connection = GetDbConnection())
            {
                connection.Execute(DeleteCharacterSlotQuery, new {Username = username, Slot = slot});
            }
        }
    }
}
