using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.EntityHealthHud
{
    public abstract class ComponentAddedOverlaySystemBase<T> : EntitySystem where T : IComponent
    {
        [Dependency] private readonly IPlayerManager _player = default!;

        protected bool IsActive = false;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<T, ComponentInit>(OnInit);
            SubscribeLocalEvent<T, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<T, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<T, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }


        public void ApplyOverlay(T component)
        {
            IsActive = true;
            OnApplyOverlay(component);
        }

        public void RemoveOverlay()
        {
            IsActive = false;
            OnRemoveOverlay();
        }

        protected virtual void OnApplyOverlay(T component) { }

        protected virtual void OnRemoveOverlay() { }

        private void OnInit(EntityUid uid, T component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                ApplyOverlay(component);
            }
        }

        private void OnRemove(EntityUid uid, T component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                RemoveOverlay();
            }
        }

        private void OnPlayerAttached(EntityUid uid, T component, PlayerAttachedEvent args)
        {
            ApplyOverlay(component);
        }

        private void OnPlayerDetached(EntityUid uid, T component, PlayerDetachedEvent args)
        {
            RemoveOverlay();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            RemoveOverlay();
        }
    }
}
