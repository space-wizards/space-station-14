using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tools.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class IngestionSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private EntityQuery<UtensilComponent> _utensilsQuery;

    public void InitializeUtensils()
    {
        base.Initialize();
        // TODO: I hate Utensils
        SubscribeLocalEvent<UtensilComponent, AfterInteractEvent>(OnAfterInteract); //, after: new[] { typeof(ItemSlotsSystem), typeof(ToolOpenableSystem) });

        _utensilsQuery = GetEntityQuery<UtensilComponent>();
    }

    /// <summary>
    /// Clicked with utensil
    /// </summary>
    private void OnAfterInteract(Entity<UtensilComponent> entity, ref AfterInteractEvent ev)
    {
        if (ev.Handled || ev.Target == null || !ev.CanReach)
            return;

        ev.Handled = TryUseUtensil(ev.User, ev.Target.Value, entity);
    }

    public bool TryUseUtensil(EntityUid user, EntityUid target, Entity<UtensilComponent> utensil)
    {
        if (!TryComp(target, out FoodComponent? food))
            return false;

        //Prevents food usage with a wrong utensil
        if ((food.Utensil & utensil.Comp.Types) == 0)
        {
            _popup.PopupClient(Loc.GetString("food-system-wrong-utensil", ("food", target), ("utensil", utensil.Owner)), user, user);
            return true;
        }

        if (!_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return true;

        return TryIngest(user, user, target);
    }

    /// <summary>
    /// Attempt to break the utensil after interaction.
    /// </summary>
    /// <param name="entity">Utensil.</param>
    /// <param name="userUid">User of the utensil.</param>
    public void TryBreak(Entity<UtensilComponent?> entity, EntityUid userUid)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!_robustRandom.Prob(entity.Comp.BreakChance))
            return;

        _audio.PlayPredicted(entity.Comp.BreakSound, userUid, userUid, AudioParams.Default.WithVolume(-2f));
        // Not prediced because no random predicted
        QueueDel(entity);
    }

    /// <summary>
    /// Checks if we have the utensils required to eat a certain food item.
    /// </summary>
    /// <param name="entity">Entity that is trying to eat.</param>
    /// <param name="component">The component of the food item we're trying to eat.</param>
    /// <param name="utensils">The utensils needed to eat the food item.</param>
    /// <returns>True if we are able to eat the item.</returns>
    private bool TryGetRequiredUtensils(Entity<HandsComponent?> entity, EdibleComponent component, out List<EntityUid> utensils)
    {
        utensils = new List<EntityUid>();

        if (component.Utensil == UtensilType.None)
            return true;

        if (!Resolve(entity, ref entity.Comp, false)) // You aren't allowed to eat with your hands in this hellish dystopia.
            return true;

        var usedTypes = UtensilType.None;

        foreach (var item in _hands.EnumerateHeld(entity))
        {
            // Is utensil?
            if (!_utensilsQuery.TryComp(item, out var utensil))
                continue;

            // Do we have a new and unused utensil type?
            if ((utensil.Types & component.Utensil) == 0 || (usedTypes & utensil.Types) == utensil.Types)
                continue;

            // Add to used list
            usedTypes |= utensil.Types;
            utensils.Add(item);
        }

        // If "required" field is set, try to block eating without proper utensils used
        if (!component.UtensilRequired || (usedTypes & component.Utensil) == component.Utensil)
            return true;

        _popup.PopupClient(Loc.GetString("food-you-need-to-hold-utensil", ("utensil", component.Utensil ^ usedTypes)), entity, entity);
        return false;

    }
}
