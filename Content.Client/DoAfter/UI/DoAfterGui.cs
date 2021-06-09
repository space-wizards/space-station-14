using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems.DoAfter
{
    public sealed class DoAfterGui : VBoxContainer
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly Dictionary<byte, PanelContainer> _doAfterControls = new();
        private readonly Dictionary<byte, DoAfterBar> _doAfterBars = new();

        // We'll store cancellations for a little bit just so we can flash the graphic to indicate it's cancelled
        private readonly Dictionary<byte, TimeSpan> _cancelledDoAfters = new();

        public IEntity? AttachedEntity { get; set; }
        private ScreenCoordinates _playerPosition;

        public DoAfterGui()
        {
            IoCManager.InjectDependencies(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(this);
            SeparationOverride = 0;

            LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (Disposed)
                return;

            foreach (var (_, control) in _doAfterControls)
            {
                control.Dispose();
            }

            _doAfterControls.Clear();
            _doAfterBars.Clear();
            _cancelledDoAfters.Clear();
        }

        /// <summary>
        ///     Add the necessary control for a DoAfter progress bar.
        /// </summary>
        /// <param name="message"></param>
        public void AddDoAfter(ClientDoAfter message)
        {
            if (_doAfterControls.ContainsKey(message.ID))
                return;

            var doAfterBar = new DoAfterBar
            {
                VerticalAlignment = VAlignment.Center
            };

            _doAfterBars[message.ID] = doAfterBar;

            var control = new PanelContainer
            {
                Children =
                {
                    new TextureRect
                    {
                        Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/Misc/progress_bar.rsi/icon.png"),
                        TextureScale = Vector2.One * DoAfterBar.DoAfterBarScale,
                        VerticalAlignment = VAlignment.Center,
                    },

                    doAfterBar
                }
            };

            AddChild(control);
            _doAfterControls.Add(message.ID, control);
        }

        // NOTE THAT THE BELOW ONLY HANDLES THE UI SIDE

        /// <summary>
        ///     Removes a DoAfter without showing a cancel graphic.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveDoAfter(byte id)
        {
            if (!_doAfterControls.ContainsKey(id))
                return;

            var control = _doAfterControls[id];
            RemoveChild(control);
            control.DisposeAllChildren();
            _doAfterControls.Remove(id);
            _doAfterBars.Remove(id);

            _cancelledDoAfters.Remove(id);
        }

        /// <summary>
        ///     Cancels a DoAfter and shows a graphic indicating it has been cancelled to the player.
        /// </summary>
        ///     Can be called multiple times on the 1 DoAfter because of the client predicting the cancellation.
        /// <param name="id"></param>
        public void CancelDoAfter(byte id)
        {
            if (_cancelledDoAfters.ContainsKey(id))
                return;

            DoAfterBar doAfterBar;

            if (!_doAfterControls.TryGetValue(id, out var doAfterControl))
            {
                doAfterControl = new PanelContainer();
                AddChild(doAfterControl);
                DebugTools.Assert(!_doAfterBars.ContainsKey(id));
                doAfterBar = new DoAfterBar();
                doAfterControl.AddChild(doAfterBar);
                _doAfterBars[id] = doAfterBar;
            }
            else
            {
                doAfterBar = _doAfterBars[id];
            }

            doAfterBar.Cancelled = true;
            _cancelledDoAfters.Add(id, _gameTiming.CurTime);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (AttachedEntity?.IsValid() != true ||
                !AttachedEntity.TryGetComponent(out DoAfterComponent? doAfterComponent))
            {
                Visible = false;
                return;
            }

            var doAfters = doAfterComponent.DoAfters;
            if (doAfters.Count == 0)
            {
                Visible = false;
                return;
            }

            if (_eyeManager.CurrentMap != AttachedEntity.Transform.MapID ||
                !AttachedEntity.Transform.Coordinates.IsValid(_entityManager))
            {
                Visible = false;
                return;
            }
            else
            {
                Visible = true;
            }

            var currentTime = _gameTiming.CurTime;
            var toRemove = new List<byte>();

            // Cleanup cancelled DoAfters
            foreach (var (id, cancelTime) in _cancelledDoAfters)
            {
                if ((currentTime - cancelTime).TotalSeconds > DoAfterSystem.ExcessTime)
                    toRemove.Add(id);
            }

            foreach (var id in toRemove)
            {
                RemoveDoAfter(id);
            }

            toRemove.Clear();

            // Update 0 -> 1.0f of the things
            foreach (var (id, message) in doAfters)
            {
                if (_cancelledDoAfters.ContainsKey(id) || !_doAfterControls.ContainsKey(id))
                    continue;

                var doAfterBar = _doAfterBars[id];
                var ratio = (currentTime - message.StartTime).TotalSeconds;
                doAfterBar.Ratio = MathF.Min(1.0f,
                    (float) ratio / message.Delay);

                // Just in case it doesn't get cleaned up by the system for whatever reason.
                if (ratio > message.Delay + DoAfterSystem.ExcessTime)
                {
                    toRemove.Add(id);
                }
            }

            foreach (var id in toRemove)
            {
                RemoveDoAfter(id);
            }

            var screenCoordinates = _eyeManager.CoordinatesToScreen(AttachedEntity.Transform.Coordinates);
            _playerPosition = new ScreenCoordinates(screenCoordinates.Position / UIScale, screenCoordinates.Window);
            LayoutContainer.SetPosition(this, new Vector2(_playerPosition.X - Width / 2, _playerPosition.Y - Height - 30.0f));
        }
    }
}
