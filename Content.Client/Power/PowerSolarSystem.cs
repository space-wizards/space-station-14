using Content.Server.Solar.Components;
using Content.Shared.Solar;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power
{
    [UsedImplicitly]
    public sealed class PowerSolarSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SolarPanelComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }

        private void OnAppearanceChange(EntityUid uid, SolarPanelComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
            {
                return;
            }

            if (!_appearanceSystem.TryGetData(uid, SolarPanelVisuals.Angle, out Angle panelAngle))
            {
                return;
            }

            if (TryComp<TransformComponent>(uid, out var xform))
            {
                args.Sprite.Rotation = panelAngle - xform.LocalRotation;
            }
        }
    }
}
