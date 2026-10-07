using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed partial class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    protected override bool CanEnable(EntityUid uid, JetpackComponent component)
    {
        // No predicted atmos so you'd have to do a lot of funny to get this working.
        return false;
    }

    [SubscribeLocalEvent]
    private void OnJetpackAppearance(Entity<JetpackComponent> ent, ref AppearanceChangeEvent args)
    {
        args.TryGetData<bool>(JetpackVisuals.Enabled, out var enabled);

        if (TryComp<ClothingComponent>(ent, out var clothing))
            _clothing.SetEquippedPrefix(ent, enabled ? "on" : null, clothing);
    }
}
