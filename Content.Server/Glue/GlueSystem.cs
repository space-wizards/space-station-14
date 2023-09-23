using Content.Server.Administration.Logs;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Item;
using Content.Shared.Glue;
using Content.Shared.Interaction;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Hands;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Components;

namespace Content.Server.Glue;

public sealed class GlueSystem : SharedGlueSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlueComponent, AfterInteractEvent>(OnInteract, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<GluedComponent, ComponentInit>(OnGluedInit);
        SubscribeLocalEvent<GluedComponent, GotEquippedHandEvent>(OnHandPickUp);
    }

    // When glue bottle is used on item it will apply the glued and unremoveable components.
    private void OnInteract(EntityUid uid, GlueComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryGlue(uid, component, target, args.User))
        {
            args.Handled = true;
            _audio.PlayPvs(component.Squeeze, uid);
            _popup.PopupEntity(Loc.GetString("glue-success", ("target", target)), args.User, args.User, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("glue-failure", ("target", target)), args.User, args.User, PopupType.Medium);
        }
    }

    private bool TryGlue(EntityUid uid, GlueComponent component, EntityUid target, EntityUid actor)
    {
        // if item is glued then don't apply glue again so it can be removed for reasonable time
        if (HasComp<GluedComponent>(target) || !HasComp<ItemComponent>(target))
        {
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            var quantity = solution.RemoveReagent(component.Reagent, component.ConsumptionUnit);
            if (quantity > 0)
            {
                EnsureComp<GluedComponent>(target).Duration = quantity.Double() * component.DurationPerUnit;
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} glued {ToPrettyString(target):subject} with {ToPrettyString(uid):tool}");
                return true;
            }
        }
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GluedComponent, UnremoveableComponent>();
        while (query.MoveNext(out var uid, out var glue, out _))
        {
            if (_timing.CurTime < glue.Until)
                continue;

            _metaData.SetEntityName(uid, glue.BeforeGluedEntityName);
            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
        }
    }

    private void OnGluedInit(EntityUid uid, GluedComponent component, ComponentInit args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;
        component.BeforeGluedEntityName = meta.EntityName;
        _metaData.SetEntityName(uid, Loc.GetString("glued-name-prefix", ("target", name)));
    }

    private void OnHandPickUp(EntityUid uid, GluedComponent component, GotEquippedHandEvent args)
    {
        EnsureComp<UnremoveableComponent>(uid);
        component.Until = _timing.CurTime + component.Duration;
    }
}
