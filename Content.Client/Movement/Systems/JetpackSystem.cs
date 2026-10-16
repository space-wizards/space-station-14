using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

public sealed partial class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private ClothingSystem _clothing = default!;

    protected override bool CanEnable(Entity<JetpackComponent> ent)
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
