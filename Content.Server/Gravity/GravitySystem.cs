using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Gravity
{
    [UsedImplicitly]
    public sealed class GravitySystem : SharedGravitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GravityComponent, ComponentInit>(OnGravityInit);
        }

        /// <summary>
        /// Iterates gravity components and checks if this entity can have gravity applied.
        /// </summary>
        public void RefreshGravity(EntityUid uid, GravityComponent? gravity = null)
        {
            if (!Resolve(uid, ref gravity))
                return;

            var enabled = false;

            foreach (var (comp, xform) in EntityQuery<GravityGeneratorComponent, TransformComponent>(true))
            {
                if (!comp.GravityActive || xform.ParentUid != uid)
                    continue;

                enabled = true;
                break;
            }

            if (enabled != gravity.Enabled)
            {
                gravity.Enabled = enabled;
                var ev = new GravityChangedEvent(uid, enabled);
                RaiseLocalEvent(uid, ref ev, true);
                Dirty(gravity);

                if (HasComp<MapGridComponent>(uid))
                {
                    StartGridShake(uid);
                }
            }
        }

        private void OnGravityInit(EntityUid uid, GravityComponent component, ComponentInit args)
        {
            RefreshGravity(uid);
        }

        public void EnableGravity(EntityUid uid, GravityComponent? gravity = null)
        {
            if (!Resolve(uid, ref gravity))
                return;

            if (gravity.Enabled)
                return;

            gravity.Enabled = true;
            var ev = new GravityChangedEvent(uid, true);
            RaiseLocalEvent(uid, ref ev, true);
            Dirty(gravity);

            if (HasComp<MapGridComponent>(uid))
            {
                StartGridShake(uid);
            }
        }
    }
}
