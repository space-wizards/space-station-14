using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent blindness, both the examine and the actual effect.
/// </summary>
public sealed class PermanentBlindnessSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blinding = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PermanentBlindnessComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PermanentBlindnessComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PermanentBlindnessComponent, RevertTraitEvent>(OnRevertTrait);
    }

    private void OnExamined(Entity<PermanentBlindnessComponent> blindness, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange && blindness.Comp.Blindness == 0)
        {
            args.PushMarkup(Loc.GetString("permanent-blindness-trait-examined", ("target", Identity.Entity(blindness, EntityManager))));
        }
    }

    private void OnShutdown(Entity<PermanentBlindnessComponent> blindness, ref ComponentShutdown args)
    {
        if (!TryComp<BlindableComponent>(blindness.Owner, out var blindable))
            return;

        if (blindable.MinDamage != 0)
        {
            _blinding.SetMinDamage((blindness.Owner, blindable), 0);
        }
    }

    private void OnMapInit(Entity<PermanentBlindnessComponent> blindness, ref MapInitEvent args)
    {
        if(!TryComp<BlindableComponent>(blindness.Owner, out var blindable))
            return;

        if (blindness.Comp.Blindness != 0)
            _blinding.SetMinDamage((blindness.Owner, blindable), blindness.Comp.Blindness);
        else
        {
            var maxMagnitudeInt = (int) BlurryVisionComponent.MaxMagnitude;
            _blinding.SetMinDamage((blindness.Owner, blindable), maxMagnitudeInt);
        }
    }

    private void OnRevertTrait(Entity<PermanentBlindnessComponent> blindness, ref RevertTraitEvent args)
    {
        if (!args.Components.Contains(blindness.Comp.GetType()))
            return;

        if(!TryComp<BlindableComponent>(blindness.Owner, out var blindable))
            return;

        _blinding.SetMinDamage((blindness.Owner, blindable), 0);
        _blinding.AdjustEyeDamage((blindness.Owner, blindable), -blindable.EyeDamage);
    }
}
