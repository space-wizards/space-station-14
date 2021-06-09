using Content.Client.GameObjects.Components.Actor;
using Robust.Client.UserInterface;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <summary>
    /// An interface which is gathered to assemble the character window from multiple components
    /// </summary>
    public interface ICharacterUI
    {
        /// <summary>
        /// The control which holds the character user interface to be included in the window
        /// </summary>
        Control Scene { get; }

        /// <summary>
        /// The order it will appear in the character UI, higher is lower
        /// </summary>
        UIPriority Priority { get; }

        /// <summary>
        /// Called when the CharacterUi was opened
        /// </summary>
        void Opened(){}
    }
}
