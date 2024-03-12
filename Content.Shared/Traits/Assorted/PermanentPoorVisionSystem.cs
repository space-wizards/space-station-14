using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent poor vision
/// </summary>
public sealed class PermanentPoorVisionSystem : EntitySystem
{

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BlindableSystem _blinding = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PermanentPoorVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PermanentPoorVisionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<PermanentPoorVisionComponent> blindness, ref ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<BlindableComponent>(blindness, out var blindable))
            return;

        _blinding.UpdateIsBlind(blindness.Owner);
        _blinding.SetMinDamage(new Entity<BlindableComponent?>(blindness.Owner, blindable), 0); // TODO replace with event as per shadowcommanders suggestion
    }

    private void OnStartup(Entity<PermanentPoorVisionComponent> blindness, ref ComponentStartup args)
    {
        if (!_entityManager.TryGetComponent<BlindableComponent>(blindness, out var blindable))
            return;

        _blinding.SetMinDamage(new Entity<BlindableComponent?>(blindness.Owner, blindable), blindness.Comp.MinDamage);
    }
}