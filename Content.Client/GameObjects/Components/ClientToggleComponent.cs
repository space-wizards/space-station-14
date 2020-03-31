using System;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class ToggleComponent : Component
    {
        public override string Name => "Toggle";
        public override uint? NetID => ContentNetIDs.TOGGLE;

        [ViewVariables] public float AmmountCapacity { get; set; }
        [ViewVariables] public float Ammount { get; set; }
        [ViewVariables] public float AmmountCost { get; set; }
        [ViewVariables] public float AmmountLossRate { get; set; }
        [ViewVariables] public bool Activated { get; set; }
        [ViewVariables] public float SoundOn { get; set; }
        [ViewVariables] public float SoundOff { get; set; }
        [ViewVariables] public float AmmountName { get; set; }
        [ViewVariables] public float AmmountColor1 { get; set; }
        [ViewVariables] public float AmmountColor2 { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ToggleComponentState cast))
                return;

            AmmountCapacity = cast.AmmountCapacity;
            Ammount = cast.Ammount;
            AmmountCost = cast.AmmountCost;
            AmmountLossRate = cast.AmmountLossRate;
            Activated = cast.Activated;
            SoundOn = cast.SoundOn;
            SoundOff = cast.SoundOff;
            AmmountName = cast.AmmountName;
            AmmountColor1 = cast.AmmountColor1;
            AmmountColor2 = cast.AmmountColor2;

            _uiUpdateNeeded = true;
        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly ToggleComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(ToggleComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {NanoStyle.StyleClassItemStatus}};
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                var ammountCap = _parent.AmmountCapacity;
                var ammount = _parent.Ammount;

                _label.SetMarkup(Loc.GetString(AmmountName+": [color={0}]{1}/{2}[/color]",
                ammount < ammountCap / 4f ? AmmountColor1 : AmmountColor2, math.round(ammount), ammountCap));
            }
        }
    }
}
