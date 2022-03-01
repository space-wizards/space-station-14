using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Guardian
{
    /// <summary>
    /// Given to guardian users upon establishing a guardian link with the entity
    /// </summary>
    [RegisterComponent]
    public sealed class GuardianHostComponent : Component
    {
        /// <summary>
        /// Guardian hosted within the component
        /// </summary>
        /// <remarks>
        /// Can be null if the component is added at any time.
        /// </remarks>
        public EntityUid? HostedGuardian;

        /// <summary>
        /// Container which holds the guardian
        /// </summary>
        [ViewVariables] public ContainerSlot GuardianContainer = default!;

        [DataField("action")]
        public InstantAction Action = new()
        {
            Name = "action-name-guardian",
            Description = "action-description-guardian",
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/manifest.png")),
            UseDelay = TimeSpan.FromSeconds(2),
            Event =  new GuardianToggleActionEvent(),
        };
    }

    public sealed class GuardianToggleActionEvent : PerformActionEvent { };
}
