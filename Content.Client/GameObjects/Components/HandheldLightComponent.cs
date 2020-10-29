using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public sealed class HandheldLightComponent : SharedHandheldLightComponent, IItemStatus
    {
        private bool _hasCell;

        [ViewVariables] public float? Charge { get; private set; }
        [ViewVariables] protected override bool HasCell => _hasCell;

        public Control MakeControl()
        {
            return new StatusControl(this);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is HandheldLightComponentState cast))
                return;

            Charge = cast.Charge;
            _hasCell = cast.HasCell;
        }

        private sealed class StatusControl : Control
        {
            private const float TimerCycle = 1;

            private readonly HandheldLightComponent _parent;
            private readonly PanelContainer[] _sections = new PanelContainer[5];

            private float _timer;

            private static readonly StyleBoxFlat _styleBoxLit = new StyleBoxFlat
            {
                BackgroundColor = Color.Green
            };

            private static readonly StyleBoxFlat _styleBoxUnlit = new StyleBoxFlat
            {
                BackgroundColor = Color.Black
            };

            public StatusControl(HandheldLightComponent parent)
            {
                _parent = parent;

                var wrapper = new HBoxContainer
                {
                    SeparationOverride = 4,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                };

                AddChild(wrapper);

                for (var i = 0; i < _sections.Length; i++)
                {
                    var panel = new PanelContainer {CustomMinimumSize = (20, 20)};
                    wrapper.AddChild(panel);
                    _sections[i] = panel;
                }
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                _timer += args.DeltaSeconds;
                _timer %= TimerCycle;

                var charge = _parent.Charge ?? 0;

                int level;

                if (MathHelper.CloseTo(charge, 0))
                {
                    level = 0;
                }
                else
                {
                    level = ContentHelpers.RoundToNearestLevels(charge, 1.0, 6) + 1;
                }

                if (level == 0)
                {
                    _sections[0].PanelOverride = _styleBoxUnlit;
                }
                else if (level == 1)
                {
                    // Flash the last light.
                    _sections[0].PanelOverride = _timer > TimerCycle / 2 ? _styleBoxLit : _styleBoxUnlit;
                }
                else
                {
                    _sections[0].PanelOverride = _styleBoxLit;
                }

                _sections[1].PanelOverride = level >= 3 ? _styleBoxLit : _styleBoxUnlit;
                _sections[2].PanelOverride = level >= 4 ? _styleBoxLit : _styleBoxUnlit;
                _sections[3].PanelOverride = level >= 5 ? _styleBoxLit : _styleBoxUnlit;
                _sections[4].PanelOverride = level >= 6 ? _styleBoxLit : _styleBoxUnlit;
            }
        }
    }
}
