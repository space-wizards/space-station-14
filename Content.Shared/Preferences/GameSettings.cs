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
