using Content.Server.Body.Components;
using Content.Shared.Humanoid;

namespace Content.Server.Body.Systems;

/// <summary>
/// Overrides slime blood color.
/// </summary>
public sealed class SlimeBloodSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeBloodComponent, BloodColorOverrideEvent>(OnBloodColorOverride);
        SubscribeLocalEvent<SlimeBloodComponent, MapInitEvent>(OnMapInit);
    }

    private void OnBloodColorOverride(Entity<SlimeBloodComponent> ent, ref BloodColorOverrideEvent args)
    {
        TryComp<HumanoidAppearanceComponent>(ent.Owner, out var appearanceComp);
        args.OverrideColor = ent.Comp.ManualOverrideColor ?? appearanceComp?.SkinColor;
    }

    private void OnMapInit(Entity<SlimeBloodComponent> ent, ref MapInitEvent args)
    {
        var ev = new RefreshBloodEvent { };
        RaiseLocalEvent(ent.Owner, ref ev);
    }
}
