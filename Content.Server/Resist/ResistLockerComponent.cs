using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Analyzers;

namespace Content.Server.Resist
{
    [RegisterComponent]
    [Friend(typeof(ResistLockerSystem))]
    public class ResistLockerComponent : Component
    {
        public override string Name => "ResistLocker";

        /// <summary>
        /// How long will this locker take to kick open, defaults to 2 minutes
        /// </summary>
        [ViewVariables]
        [DataField("resistTime")]
        public float ResistTime = 120f;

        /// <summary>
        /// For quick exit if the player attempts to move while already resisting
        /// </summary>
        [ViewVariables]
        public bool IsResisting = false;

    }
}
