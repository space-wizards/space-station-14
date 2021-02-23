using Content.Shared.GameObjects.Components;
using Robust.Client.Graphics;
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
        [ViewVariables] protected override bool HasCell => _level != null;

        private byte? _level;

        public Control MakeControl()
        {
            return new StatusControl(this);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not HandheldLightComponentState cast)
                return;

            _level = cast.Charge;
        }

        private sealed class StatusControl : Control
        {
            private const float TimerCycle = 1;

            private readonly HandheldLightComponent _parent;
            private readonly PanelContainer[] _sections = new PanelContainer[StatusLevels - 1];

            private float _timer;

            private static readonly StyleBoxFlat _styleBoxLit = new()
            {
                BackgroundColor = Color.Green
            };

            private static readonly StyleBoxFlat _styleBoxUnlit = new()
            {
                BackgroundColor = Color.Black
            };

            public StatusControl(HandheldLightComponent parent)
            {
                _parent = parent;

                var wrapper = new HBoxContainer
                {
                    SeparationOverride = 4,
                    HorizontalAlignment = HAlignment.Center
                };

                AddChild(wrapper);

                for (var i = 0; i < _sections.Length; i++)
                {
                    var panel = new PanelContainer {MinSize = (20, 20)};
                    wrapper.AddChild(panel);
                    _sections[i] = panel;
                }
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                if (!_parent.HasCell)
                    return;

                _timer += args.DeltaSeconds;
                _timer %= TimerCycle;

                var level = _parent._level;

                for (var i = 0; i < _sections.Length; i++)
                {
                    if (i == 0)
                    {
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

                        continue;
                    }

                    _sections[i].PanelOverride = level >= i + 2 ? _styleBoxLit : _styleBoxUnlit;
                }
            }
        }
    }
}
