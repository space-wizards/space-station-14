// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Information needed for character setup.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GameSettings
    {
        private int _maxCharacterSlots;

        public int MaxCharacterSlots
        {
            get => _maxCharacterSlots;
            set => _maxCharacterSlots = value;
        }
    }
}
