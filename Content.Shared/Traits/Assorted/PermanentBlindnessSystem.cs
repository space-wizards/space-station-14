using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent blindness, both the examine and the actual effect.
/// </summary>
public sealed class PermanentBlindnessSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BlindableSystem _blinding = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PermanentBlindnessComponent, EyeDamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PermanentBlindnessComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<PermanentBlindnessComponent> blindness, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !_net.IsClient)
        {
            args.PushMarkup(Loc.GetString("permanent-blindness-trait-examined", ("target", Identity.Entity(blindness, EntityManager))));
        }
    }

    private void OnShutdown(Entity<PermanentBlindnessComponent> blindness, ref ComponentShutdown args)
    {
        _blinding.UpdateIsBlind(blindness.Owner);
    }

    private void OnStartup(Entity<PermanentBlindnessComponent> blindness, ref ComponentStartup args)
    {
        if (!_entityManager.TryGetComponent<BlindableComponent>(blindness, out var blindable))
            return;

        var damageToDeal = (int) BlurryVisionComponent.MaxMagnitude - blindable.EyeDamage;

        if (damageToDeal <= 0)
            return;

        _blinding.AdjustEyeDamage(blindness.Owner, damageToDeal);
    }

    private void OnDamageChanged(Entity<PermanentBlindnessComponent> blindness, ref EyeDamageChangedEvent args)
    {
        if (args.Damage >= BlurryVisionComponent.MaxMagnitude)
            return;

        if (!_entityManager.TryGetComponent<BlindableComponent>(blindness, out var blindable))
            return;

        var damageRestoration = (int) BlurryVisionComponent.MaxMagnitude - args.Damage;
        _blinding.AdjustEyeDamage(blindness.Owner, damageRestoration);
    }
}
