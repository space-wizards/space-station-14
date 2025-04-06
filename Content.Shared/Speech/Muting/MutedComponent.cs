using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Muting
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MutedComponent : Component
    {
        // imp change
        /// <summary>
        /// Whether or not the affected entity can speak.
        /// </summary>
        [DataField(serverOnly: true)]
        public bool MutedSpeech = true;

        // imp change
        /// <summary>
        /// Whether or not the affected entity emotes will have sound.
        /// </summary>
        [DataField(serverOnly: true)]
        public bool MutedEmotes = true;

        // imp change
        /// <summary>
        /// Whether or not the affected entity will be able to scream.
        /// </summary>
        [DataField(serverOnly: true)]
        public bool MutedScream = true;
    }
}
