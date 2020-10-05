using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private List<ICharacterProfile> _characters;

        public PlayerPreferences(IEnumerable<ICharacterProfile> characters, int selectedCharacterIndex)
        {
            _characters = characters.ToList();
            SelectedCharacterIndex = selectedCharacterIndex;
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IEnumerable<ICharacterProfile> Characters => _characters.AsEnumerable();

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters.ElementAtOrDefault(SelectedCharacterIndex);

        public int FirstEmptySlot()
        {
            var firstEmpty = IndexOfCharacter(null);
            return firstEmpty == -1 ? _characters.Count : firstEmpty;
        }

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FindIndex(x => x == profile);
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }
    }
}
