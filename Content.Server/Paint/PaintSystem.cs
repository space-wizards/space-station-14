using Content.Shared.Popups;
using Content.Shared.Paint;
using Content.Shared.Sprite;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using Robust.Shared.Utility;
using Content.Shared.Verbs;
using Content.Shared.SubFloor;
using Content.Server.Nutrition.Components;
using Content.Shared.Inventory;
using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public sealed class PaintSystem : SharedPaintSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PaintComponent, PaintDoAfterEvent>(OnPaint);
        SubscribeLocalEvent<PaintComponent, GetVerbsEvent<UtilityVerb>>(OnPaintVerb);
    }

    private void OnInteract(EntityUid uid, PaintComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        PrepPaint(uid, component, target, args.User);
    }

    private void OnPaintVerb(EntityUid uid, PaintComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var paintText = Loc.GetString("paint-verb");

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                PrepPaint(uid, component, args.Target, args.User);
            },

            Text = paintText,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/paint.svg.192dpi.png"))
        };
        args.Verbs.Add(verb);
    }
    private void PrepPaint(EntityUid uid, PaintComponent component, EntityUid target, EntityUid user)
    {

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.Delay, new PaintDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnPaint(Entity<PaintComponent> entity, ref PaintDoAfterEvent args)
    {
        if (args.Target == null || args.Used == null)
            return;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!_openable.IsOpen(entity))
        {
            _popup.PopupEntity(Loc.GetString("paint-closed", ("used", args.Used)), args.User, args.User, PopupType.Medium);
            return;
        }

        if (HasComp<PaintedComponent>(target) || HasComp<RandomSpriteComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("paint-failure-painted", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!entity.Comp.Blacklist?.IsValid(target, EntityManager) != true || HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("paint-failure", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            return;
        }

        if (TryPaint(entity, target))
        {
            EnsureComp<PaintedComponent>(target, out PaintedComponent? paint);
            EnsureComp<AppearanceComponent>(target);

            paint.Color = entity.Comp.Color; // set the target color to the color specified in the spray paint yml.
            _audio.PlayPvs(entity.Comp.Spray, entity);
            paint.Enabled = true;

            if (HasComp<InventoryComponent>(target)) // Paint any clothing the target is wearing.
            {
                if (_inventory.TryGetSlots(target, out var slotDefinitions))
                {
                    foreach (var slot in slotDefinitions)
                    {
                        if (!_inventory.TryGetSlotEntity(target, slot.Name, out var slotEnt))
                            continue;

                        if (HasComp<PaintedComponent>(slotEnt.Value) || !entity.Comp.Blacklist?.IsValid(slotEnt.Value,
                                                                         EntityManager) != true
                                                                     || HasComp<RandomSpriteComponent>(slotEnt.Value) ||
                                                                     HasComp<HumanoidAppearanceComponent>(
                                                                         slotEnt.Value))
                        {
                            continue;
                        }

                        EnsureComp<PaintedComponent>(slotEnt.Value, out PaintedComponent? slotpaint);
                        EnsureComp<AppearanceComponent>(slotEnt.Value);
                        slotpaint.Color = entity.Comp.Color;
                        _appearanceSystem.SetData(slotEnt.Value, PaintVisuals.Painted, true);
                        Dirty(slotEnt.Value, slotpaint);
                    }
                }
            }

            _popup.PopupEntity(Loc.GetString("paint-success", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            _appearanceSystem.SetData(target, PaintVisuals.Painted, true);
            Dirty(target, paint);
            args.Handled = true;
            return;
        }

        if (!TryPaint(entity, target))
        {
            _popup.PopupEntity(Loc.GetString("paint-empty", ("used", args.Used)), args.User, args.User, PopupType.Medium);
            return;
        }
    }

    private bool TryPaint(Entity<PaintComponent> reagent, EntityUid target)
    {
        if (HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target))
            return false;

        if (_solutionContainer.TryGetSolution(reagent.Owner, reagent.Comp.Solution, out _, out var solution))
        {
            var quantity = solution.RemoveReagent(reagent.Comp.Reagent, reagent.Comp.ConsumptionUnit);
            if (quantity > 0)// checks quantity of solution is more than 0.
                return true;

            if (quantity < 1)
                return false;
        }
        return false;
    }
}
