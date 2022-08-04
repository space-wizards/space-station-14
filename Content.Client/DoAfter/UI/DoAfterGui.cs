using Content.Shared.DoAfter;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.DoAfter.UI
{
    public sealed class DoAfterGui : BoxContainer
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly Dictionary<byte, DoAfterControl> _doAfterControls = new();

        // We'll store cancellations for a little bit just so we can flash the graphic to indicate it's cancelled
        private readonly Dictionary<byte, TimeSpan> _cancelledDoAfters = new();

        public EntityUid? AttachedEntity { get; set; }

        public DoAfterGui()
        {
            Orientation = LayoutOrientation.Vertical;

            IoCManager.InjectDependencies(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(this);
            SeparationOverride = 0;
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
            _cancelledDoAfters.Clear();
        }

        /// <summary>
        ///     Add the necessary control for a DoAfter progress bar.
        /// </summary>
        public void AddDoAfter(ClientDoAfter message)
        {
            if (_doAfterControls.ContainsKey(message.ID))
                return;

            var doAfterBar = new DoAfterControl();
            AddChild(doAfterBar);
            _doAfterControls.Add(message.ID, doAfterBar);
            Measure(Vector2.Infinity);
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

            if (!_doAfterControls.TryGetValue(id, out var doAfterControl))
            {
                doAfterControl = new DoAfterControl();
                AddChild(doAfterControl);
            }

            doAfterControl.Cancelled = true;
            _cancelledDoAfters.Add(id, _gameTiming.CurTime);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_entityManager.TryGetComponent(AttachedEntity, out DoAfterComponent? doAfterComponent))
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

            var transform = _entityManager.GetComponent<TransformComponent>(AttachedEntity.Value);

            if (_eyeManager.CurrentMap != transform.MapID || !transform.Coordinates.IsValid(_entityManager))
            {
                Visible = false;
                return;
            }

            Visible = true;
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

                var control = _doAfterControls[id];
                var ratio = (currentTime - message.StartTime).TotalSeconds;
                control.Ratio = MathF.Min(1.0f,
                    (float) ratio / message.Delay);

                // Just in case it doesn't get cleaned up by the system for whatever reason.
                if (ratio > message.Delay + DoAfterSystem.ExcessTime)
                {
                    toRemove.Add(id);
                    continue;
                }
            }

            foreach (var id in toRemove)
            {
                RemoveDoAfter(id);
            }

            UpdatePosition(transform);
        }

        public void UpdatePosition(TransformComponent xform)
        {
            var screenCoordinates = _eyeManager.CoordinatesToScreen(xform.Coordinates);
            var position = screenCoordinates.Position / UIScale - DesiredSize / 2f;
            LayoutContainer.SetPosition(this, position - new Vector2(0, 40f));
        }
    }
}
