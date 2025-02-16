using Content.Server.Flash;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Server._Starlight.Overlay;
public sealed class VisionsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, FlashAttemptEvent>(Uncancel, after: [typeof(FlashSystem)]);
        SubscribeLocalEvent<NightVisionComponent, FlashAttemptEvent>(Uncancel, after: [typeof(FlashSystem)]);
        SubscribeLocalEvent<CycloritesVisionComponent, FlashAttemptEvent>(Uncancel, after: [typeof(FlashSystem)]);
    }

    private static void Uncancel<T>(Entity<T> ent, ref FlashAttemptEvent args) where T : IComponent => args.Uncancel();
}
