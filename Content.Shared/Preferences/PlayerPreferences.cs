using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Contains all player characters and the index of the currently selected character.
    /// Serialized both over the network and to disk.
    /// </summary>
    [Serializable, NetSerializable]
    public class PlayerPreferences
    {
        public static PlayerPreferences Default()
        {
            return new PlayerPreferences
            {
                Characters = new List<ICharacterProfile>
                {
                    HumanoidCharacterProfile.Default()
                },
                SelectedCharacterIndex = 0
            };
        }

        private List<ICharacterProfile> _characters;
        private int _selectedCharacterIndex;

        /// <summary>
        /// All player characters.
        /// </summary>
        public List<ICharacterProfile> Characters
        {
            get => _characters;
            set => _characters = value;
        }

        /// <summary>
        /// Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex
        {
            get => _selectedCharacterIndex;
            set => _selectedCharacterIndex = value;
        }

        /// <summary>
        /// Retrieves the currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters.ElementAtOrDefault(SelectedCharacterIndex);
    }
}
