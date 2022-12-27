using Content.Shared.Atmos.Piping;
using Content.Shared.Solar;
using Content.Shared.VendingMachines;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                args.Sprite.Rotation = panelAngle - xform.WorldRotation;
        }
    }
}
