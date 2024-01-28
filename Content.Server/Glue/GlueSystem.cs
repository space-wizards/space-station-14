using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Glue;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

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
    private void OnInteract(Entity<GlueComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryGlue(entity, target, args.User))
        {
            args.Handled = true;
            _audio.PlayPvs(entity.Comp.Squeeze, entity);
            _popup.PopupEntity(Loc.GetString("glue-success", ("target", target)), args.User, args.User, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("glue-failure", ("target", target)), args.User, args.User, PopupType.Medium);
        }
    }

    private bool TryGlue(Entity<GlueComponent> glue, EntityUid target, EntityUid actor)
    {
        // if item is glued then don't apply glue again so it can be removed for reasonable time
        if (HasComp<GluedComponent>(target) || !HasComp<ItemComponent>(target))
        {
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(glue.Owner, glue.Comp.Solution, out _, out var solution))
        {
            var quantity = solution.RemoveReagent(glue.Comp.Reagent, glue.Comp.ConsumptionUnit);
            if (quantity > 0)
            {
                EnsureComp<GluedComponent>(target).Duration = quantity.Double() * glue.Comp.DurationPerUnit;
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} glued {ToPrettyString(target):subject} with {ToPrettyString(glue.Owner):tool}");
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

    private void OnGluedInit(Entity<GluedComponent> entity, ref ComponentInit args)
    {
        var meta = MetaData(entity);
        var name = meta.EntityName;
        entity.Comp.BeforeGluedEntityName = meta.EntityName;
        _metaData.SetEntityName(entity.Owner, Loc.GetString("glued-name-prefix", ("target", name)));
    }

    private void OnHandPickUp(Entity<GluedComponent> entity, ref GotEquippedHandEvent args)
    {
        var comp = EnsureComp<UnremoveableComponent>(entity);
        comp.DeleteOnDrop = false;
        entity.Comp.Until = _timing.CurTime + entity.Comp.Duration;
    }
}
