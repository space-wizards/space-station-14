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
        SubscribeLocalEvent<SlimeBloodComponent, LoadedHumanoidAppearanceEvent>(OnAppearanceLoaded);
    }

    private void OnBloodColorOverride(Entity<SlimeBloodComponent> ent, ref BloodColorOverrideEvent args)
    {
        TryComp<HumanoidAppearanceComponent>(ent.Owner, out var appearanceComp);
        args.OverrideColor = ent.Comp.ManualOverrideColor ?? appearanceComp?.SkinColor;
    }

    private void OnAppearanceLoaded(Entity<SlimeBloodComponent> entity, ref LoadedHumanoidAppearanceEvent ev)
    {
        var refreshBlood = new RefreshBloodEvent { };
        RaiseLocalEvent(entity.Owner, ref refreshBlood);
    }
}
