using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Revenant.Components;
using Content.Shared.Alert;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.StatusEffect;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantStasisSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRoles = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string RevenantStasisId = "Stasis";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantStasisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantStasisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RevenantStasisComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<RevenantStasisComponent, ChangeDirectionAttemptEvent>(OnAttemptDirection);
        SubscribeLocalEvent<RevenantStasisComponent, ExaminedEvent>(OnExamine);
    }

    private void OnStartup(EntityUid uid, RevenantStasisComponent component, ComponentStartup args)
    {
        EnsureComp<AlertsComponent>(uid);

        EnsureComp<StatusEffectsComponent>(uid);
        _statusEffects.TryAddStatusEffect(uid, RevenantStasisId, component.StasisDuration, true);

        var mover = EnsureComp<InputMoverComponent>(uid);
        mover.CanMove = false;
        Dirty(uid, mover);

        if (TryComp<GhostRoleComponent>(uid, out var ghostRole))
            _ghostRoles.UnregisterGhostRole((uid, ghostRole));
    }

    private void OnShutdown(EntityUid uid, RevenantStasisComponent component, ComponentShutdown args)
    {
        if (_statusEffects.HasStatusEffect(uid, RevenantStasisId))
        {
            if (_mind.TryGetMind(uid, out var mindId, out var _))
                _mind.TransferTo(mindId, null);
            QueueDel(component.Revenant);
        }
    }

    private void OnStatusEnded(EntityUid uid, RevenantStasisComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stasis")
        {
            _transformSystem.SetCoordinates(component.Revenant, Transform(uid).Coordinates);
            _transformSystem.AttachToGridOrMap(component.Revenant);
            _meta.SetEntityPaused(component.Revenant, false);
            if (_mind.TryGetMind(uid, out var mindId, out var _))
                _mind.TransferTo(mindId, component.Revenant);
            QueueDel(uid);
        }
    }

    private void OnExamine(Entity<RevenantStasisComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("revenant-stasis-regenerating"));
    }

    private void OnAttemptDirection(EntityUid uid, RevenantStasisComponent comp, ChangeDirectionAttemptEvent args)
    {
        args.Cancel();
    }
}
