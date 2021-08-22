using System;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Shared.Light;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Light.EntitySystems
{
    public class PoweredLightSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // time to blink light when ghost made boo nearby
        private static readonly TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
        }

        private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
        {
            if (light.IgnoreGhostsBoo)
                return;

            // check cooldown first to prevent abuse
            var time = _gameTiming.CurTime;
            if (light.LastGhostBlink != null)
            {
                if (time <= light.LastGhostBlink + GhostBlinkingCooldown)
                    return;
            }

            light.LastGhostBlink = time;

            ToggleBlinkingLight(light, true);
            light.Owner.SpawnTimer(GhostBlinkingTime, () =>
            {
                ToggleBlinkingLight(light, false);
            });

            args.Handled = true;
        }

        public void ToggleBlinkingLight(PoweredLightComponent light, bool isNowBlinking)
        {
            if (light.IsBlinking == isNowBlinking)
                return;

            light.IsBlinking = isNowBlinking;

            if (!light.Owner.TryGetComponent(out AppearanceComponent? appearance))
                return;
            appearance.SetData(PoweredLightVisuals.Blinking, isNowBlinking);
        }
    }
}
