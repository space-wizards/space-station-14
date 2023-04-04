using Content.Client.IconSmoothing;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Fluids
{
    [UsedImplicitly]
    public sealed class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly IconSmoothSystem _smooth = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PuddleComponent, AppearanceChangeEvent>(OnPuddleAppearance);
        }

        private void OnPuddleAppearance(EntityUid uid, PuddleComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            float volume = 1f;

            if (args.AppearanceData.TryGetValue(PuddleVisuals.CurrentVolume, out var volumeObj))
            {
                volume = (float) volumeObj;
            }

            TryComp<IconSmoothComponent>(uid, out var smooth);

            // TODO: Update sprite / smoothing based on volume
            if (volume < 0.2f)
            {
                _smooth.SetEnabled(uid, false, smooth);
            }
            else if (volume < 0.5f)
            {
                _smooth.SetEnabled(uid, false, smooth);
            }
            else
            {
                _smooth.SetEnabled(uid, true, smooth);
            }
        }
    }
}
