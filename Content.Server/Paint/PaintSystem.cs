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
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            return;
    }


    private void OnPaint(Entity<PaintComponent> entity, ref PaintDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        if (args.Target is not { Valid: true } target)
            return;

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
            _popup.PopupEntity(Loc.GetString("paint-success", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            _appearanceSystem.SetData(target, PaintVisuals.Painted, true);
            Dirty(target, paint);
            args.Handled = true;
            return;
        }
        if (args.Used == null)
            return;

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
