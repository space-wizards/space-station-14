using Content.Shared.Eye;
using Content.Shared.StatusEffectNew;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Eye.EntitySystems;

/// <summary>
/// Refreshes visibility layer modifiers contributed by active status effects.
/// </summary>
public sealed class VisibilityModifierStatusSystem : EntitySystem
{
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<VisibilityModifierStatusComponent, StatusEffectAppliedEvent>(OnStatusApplied);
        SubscribeLocalEvent<VisibilityModifierStatusComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
        SubscribeLocalEvent<VisibilityModifierStatusComponent, StatusEffectRelayedEvent<RefreshVisibilityModifiersEvent>>(OnRefreshVisibilityModifiers);
    }

    private void OnStatusApplied(Entity<VisibilityModifierStatusComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _visibility.RefreshVisibility(args.Target);
    }

    private void OnStatusRemoved(Entity<VisibilityModifierStatusComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _visibility.RefreshVisibility(args.Target);
    }

    private void OnRefreshVisibilityModifiers(
        Entity<VisibilityModifierStatusComponent> ent,
        ref StatusEffectRelayedEvent<RefreshVisibilityModifiersEvent> args)
    {
        var ev = args.Args;

        foreach (var layer in ent.Comp.AddVisibility)
        {
            ev.AddLayer((ushort) layer);
        }

        foreach (var layer in ent.Comp.RemoveVisibility)
        {
            ev.RemoveLayer((ushort) layer);
        }

        args.Args = ev;
    }
}
