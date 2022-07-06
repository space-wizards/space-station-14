using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Server.Abilities.Librarian
{
    [RegisterComponent]
    public sealed class LibrarianPowersComponent : Component
    {
        /// <summary>
        /// The range of the librarian's domain, measured from the librarian spawn
        /// </summary>
        [DataField("libraryDomainRange")]
        public float LibraryDomainRange = 15f;

        /// <summary>
        /// Total time the target will be muted
        /// </summary>
        [DataField("shushTime")]
        public TimeSpan ShushTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Accumulator to time the removal of the muted effect
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("shushAction")]
        public EntityTargetAction ShushAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(120),
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Alerts/Abilities/silenced.png")),
            Name = "librarian-shush",
            Description = "librarian-shush-desc",
            Priority = -1,
            CanTargetSelf = false,
            Event = new ShushActionEvent(),
        };

        [ViewVariables]
        [DataField("enabled")]
        public bool Enabled = false;

        public EntityUid? ShushedEntity;
    }
}
