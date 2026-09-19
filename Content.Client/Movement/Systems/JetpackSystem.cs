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
        Appearance.TryGetData<bool>(ent.Owner, JetpackVisuals.Enabled, out var enabled, args.Component);

        if (TryComp<ClothingComponent>(ent.Owner, out var clothing))
            _clothing.SetEquippedPrefix(ent.Owner, enabled ? "on" : null, clothing);
    }
}
