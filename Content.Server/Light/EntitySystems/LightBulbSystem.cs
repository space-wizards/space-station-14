using Content.Server.Light.Components;
using Content.Server.Light.Events;
using Content.Shared.Light;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using System;

namespace Content.Server.Light.EntitySystems
{
    public sealed class LightBulbSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightBulbComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LightBulbComponent, LandEvent>(HandleLand);
        }

        private void OnInit(EntityUid uid, LightBulbComponent bulb, ComponentInit args)
        {
            // update default state of bulbs
            SetColor(uid, bulb.Color, bulb);
            SetState(uid, bulb.State, bulb);
        }

        private void HandleLand(EntityUid uid, LightBulbComponent bulb, LandEvent args)
        {
            PlayBreakSound(uid, bulb);
            bulb.State = LightBulbState.Broken;
        }

        public void SetColor(EntityUid uid, Color color, LightBulbComponent? bulb = null)
        {
            if (!Resolve(uid, ref bulb))
                return;

            bulb.Color = color;
            UpdateAppearance(uid, bulb);

            RaiseLocalEvent(uid, new BulbColorChangedEvent(uid, color));
        }

        public void SetState(EntityUid uid, LightBulbState state, LightBulbComponent? bulb = null)
        {
            if (!Resolve(uid, ref bulb))
                return;

            bulb.State = state;
            UpdateAppearance(uid, bulb);

            RaiseLocalEvent(uid, new BulbStateChangedEvent(uid, state));
        }

        public void PlayBreakSound(EntityUid uid, LightBulbComponent? bulb = null)
        {
            if (!Resolve(uid, ref bulb))
                return;

            SoundSystem.Play(Filter.Pvs(uid), bulb.BreakSound.GetSound(), uid);
        }

        private void UpdateAppearance(EntityUid uid, LightBulbComponent? bulb = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref bulb, ref appearance))
                return;

            // try to update appearance and color
            appearance.SetData(LightBulbVisuals.State, bulb.State);
            appearance.SetData(LightBulbVisuals.Color, bulb.Color);
        }
    }
}
