using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Clothing.Components
{
    [Access(typeof(MaskSystem))]
    [RegisterComponent]
    public sealed class MaskComponent : Component
    {
        /// <summary>
        /// This mask can be toggled (pulled up/down)
        /// </summary>
        [DataField("toggleAction")]
        public InstantAction? ToggleAction = null;

        /// <summary>
        /// When triggered, make the wearer say something.
        /// </summary>
        [DataField("maskShoutAction")]
        public InstantAction? MaskShoutAction = null;

        public bool IsToggled = false;
    }

    public sealed class ToggleMaskEvent : InstantActionEvent { }

    public sealed class MaskShoutEvent : InstantActionEvent { }
}
