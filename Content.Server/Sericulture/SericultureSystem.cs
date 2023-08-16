using Content.Shared.Sericulture;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Sericulture;

/// <summary>
/// Allows mobs to produce materials with <see cref="SericultureComponent"/>.
/// </summary>
public sealed partial class SericultureSystem : SharedSericultureSystem
{
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SericultureComponent, SericultureDoAfterEvent>(OnSericultureDoAfter);
    }

    private void OnSericultureDoAfter(EntityUid uid, SericultureComponent comp, SericultureDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

        if (_hungerSystem.IsHungerBelowState(uid, comp.MinHungerThreshold)) // A check, just incase the doafter is somehow preformed when the entity is not in the right hunger state.
        {
            _popupSystem.PopupEntity(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        _hungerSystem.ModifyHunger(uid, -comp.HungerCost);

        var newEntity = Spawn(comp.EntityProduced, Transform(uid).Coordinates);

        _stackSystem.TryMergeToHands(newEntity, uid);

        // Make it repeat for that lil QoL.
        args.Repeat = true;
    }
}
