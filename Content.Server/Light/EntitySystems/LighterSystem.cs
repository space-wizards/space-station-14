using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.Lighter;
using Content.Shared.ActionBlocker;

namespace Content.Server.Lighter
{
    public class LighterSystem : SharedLighterSystem
    {
        private readonly HashSet<EntityUid> _activeLighters = new();
        [Dependency]
        private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LighterComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<LighterComponent, UseInHandEvent>(OnUse);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var item in _activeLighters.ToArray())
            {
                if (!EntityManager.TryGetComponent(item, out LighterComponent? lighter))
                    continue;
                if (!lighter.Lit)
                    continue;
                _atmosphereSystem.HotspotExpose(lighter.Owner.Transform.Coordinates, 400, 50, true);
                lighter.Dirty();
            }
        }

        private void OnUse(EntityUid uid, LighterComponent lighter, UseInHandEvent args)
        {
            if (!Get<ActionBlockerSystem>().CanUse(args.User.Uid))
                return;

            TryToggleLighter(uid, args.User.Uid, lighter);
        }

        private bool TryToggleLighter(EntityUid uid, EntityUid? user, LighterComponent? lighter,
            ItemComponent? item = null,
            PointLightComponent? light = null,
            AppearanceComponent? sprite = null)
        {

            if (!Resolve(uid, ref lighter))
                return false;

            return !lighter.Lit
             ? TryTurnLighterOn(uid, user, lighter, item, light, sprite)
             : TryTurnLighterOff(uid, user, lighter, item, light, sprite);
        }

        private void OnLighterUseInHand(EntityUid uid, LighterComponent lighter, UseInHandEvent args)
        {
            args.Handled = TryToggleLighter(uid, args.User.Uid, lighter);
        }
        private bool TryTurnLighterOn(EntityUid uid, EntityUid? user,
            LighterComponent? lighter = null,
            ItemComponent? item = null,
            PointLightComponent? light = null,
            AppearanceComponent? sprite = null)
        {
            if (!Resolve(uid, ref lighter, ref sprite))
                return false;

            Resolve(uid, ref item, ref light);

            lighter.Lit = true;

            if (item != null)
                item.EquippedPrefix = "on";

            sprite.SetData(LighterVisuals.Status, LighterStatus.On);

            if (light != null)
                light.Enabled = true;

            _atmosphereSystem.HotspotExpose(lighter.Owner.Transform.Coordinates, 700, 50, true);

            lighter.Dirty();
            _activeLighters.Add(uid);
            return true;
        }

        private bool TryTurnLighterOff(EntityUid uid, EntityUid? user,
            LighterComponent? lighter = null,
            ItemComponent? item = null,
            PointLightComponent? light = null,
            AppearanceComponent? sprite = null)
        {
            if (!Resolve(uid, ref lighter, ref sprite))
                return false;

            Resolve(uid, ref item, ref light);

            lighter.Lit = false;

            if (item != null)
                item.EquippedPrefix = "off";

            sprite.SetData(LighterVisuals.Status, LighterStatus.Off);

            if (light != null)
                light.Enabled = false;

            lighter.Dirty();
            _activeLighters.Remove(uid);
            return true;
        }
        private void OnIsHotEvent(EntityUid uid, LighterComponent component, IsHotEvent args)
        {
            args.IsHot = component.Lit;
        }
    }
}
