using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Chemistry.Components
{
    /// <summary>
    /// Client behavior for injectors & syringes. Used for item status on injectors
    /// </summary>
    [RegisterComponent]
    public class InjectorComponent : SharedInjectorComponent, IItemStatus
    {
        [ViewVariables] private FixedPoint2 CurrentVolume { get; set; }
        [ViewVariables] private FixedPoint2 TotalVolume { get; set; }
        [ViewVariables] private InjectorToggleMode CurrentMode { get; set; }
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;

        //Add/remove item status code
        Control IItemStatus.MakeControl() => new StatusControl(this);
        void IItemStatus.DestroyControl(Control control) { }

        //Handle net updates
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not InjectorComponentState state)
            {
                return;
            }

            CurrentVolume = state.CurrentVolume;
            TotalVolume = state.TotalVolume;
            CurrentMode = state.CurrentMode;
            _uiUpdateNeeded = true;
        }

        /// <summary>
        /// Item status control for injectors
        /// </summary>
        private sealed class StatusControl : Control
        {
            private readonly InjectorComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(InjectorComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);
                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                //Update current volume and injector state
                var modeStringLocalized = _parent.CurrentMode switch
                {
                    InjectorToggleMode.Draw => Loc.GetString("injector-draw-text"),
                    InjectorToggleMode.Inject => Loc.GetString("injector-inject-text"),
                    _ => Loc.GetString("injector-invalid-injector-toggle-mode")
                };
                _label.SetMarkup(Loc.GetString("injector-volume-label",
                                               ("currentVolume", _parent.CurrentVolume),
                                               ("totalVolume", _parent.TotalVolume),
                                               ("modeString", modeStringLocalized)));
            }
        }
    }
}
