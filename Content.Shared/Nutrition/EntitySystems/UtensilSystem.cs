using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class UtensilSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly FoodSystem _foodSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UtensilComponent, AfterInteractEvent>(OnAfterInteract, after: new[] { typeof(ItemSlotsSystem), typeof(ToolOpenableSystem) });
    }

    /// <summary>
    /// Clicked with utensil
    /// </summary>
    private void OnAfterInteract(Entity<UtensilComponent> entity, ref AfterInteractEvent ev)
    {
        if (ev.Handled || ev.Target == null || !ev.CanReach)
            return;

        var result = TryUseUtensil(ev.User, ev.Target.Value, entity);
        ev.Handled = result.Handled;
    }

    public (bool Success, bool Handled) TryUseUtensil(EntityUid user, EntityUid target, Entity<UtensilComponent> utensil)
    {
        if (!EntityManager.TryGetComponent(target, out FoodComponent? food))
            return (false, false);

        //Prevents food usage with a wrong utensil
        if ((food.Utensil & utensil.Comp.Types) == 0)
        {
            _popupSystem.PopupClient(Loc.GetString("food-system-wrong-utensil", ("food", target), ("utensil", utensil.Owner)), user, user);
            return (false, true);
        }

        if (!_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return (false, true);

        return _foodSystem.TryFeed(user, user, target, food);
    }

    /// <summary>
    /// Attempt to break the utensil after interaction.
    /// </summary>
    /// <param name="uid">Utensil.</param>
    /// <param name="userUid">User of the utensil.</param>
    public void TryBreak(EntityUid uid, EntityUid userUid, UtensilComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_robustRandom.Prob(component.BreakChance))
        {
            _audio.PlayPredicted(component.BreakSound, userUid, userUid, AudioParams.Default.WithVolume(-2f));
            EntityManager.DeleteEntity(uid);
        }
    }
}
