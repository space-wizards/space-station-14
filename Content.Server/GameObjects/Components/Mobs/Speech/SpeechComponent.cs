using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    internal interface IAccentComponent
    {
        /// <summary>
        /// Transforms a message with the given Accent
        /// </summary>
        /// <param name="message">The spoken message</param>
        /// <returns>The message after the transformation</returns>
        public string Accentuate(string message);
    }

    /// <summary>
    ///     Component required for entities to be able to speak.
    /// </summary>
    [RegisterComponent]
    public class SpeechComponent : Component, IActionBlocker
    {
        public override string Name => "Speech";

        public bool Enabled { get; set; } = true;

        bool IActionBlocker.CanSpeak() => Enabled;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Enabled, "enabled", true);
        }
    }
}
