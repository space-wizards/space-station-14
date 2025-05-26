using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Ghost;
using Content.Server.Silicons.Borgs;
using Content.Shared.Body.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Research.Prototypes;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Xenoborgs;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Player;

namespace Content.Server.Xenoborgs;

public sealed class XenoborgFactorySystem : SharedXenoborgFactorySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!; //bobby
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly BorgSystem _borg = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoborgFactoryComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
    }

    /// <inheritdoc/>
    protected override void Reclaim(EntityUid uid, EntityUid item, XenoborgFactoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.Reclaim(uid, item, component);

        FinishProducing(uid, item);

        var logImpact = HasComp<HumanoidAppearanceComponent>(item) ? LogImpact.Extreme : LogImpact.Medium;
        _adminLogger.Add(LogType.Gib,
            logImpact,
            $"{ToPrettyString(item):victim} was gibbed by {ToPrettyString(uid):entity} ");
        _body.GibBody(item);


        Del(item);
    }

    private void FinishProducing(EntityUid uid, EntityUid item, XenoborgFactoryComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;
        if (!Proto.TryIndex(comp.Recipe, out LatheRecipePrototype? recipe))
            return;
        if (recipe.Result is { } resultProto)
        {
            var result = Spawn(resultProto, Transform(uid).Coordinates);
            BorgChassisComponent? chassis = null;
            EntityUid? brain = null;
            foreach (var (id, _) in _body.GetBodyOrgans(item))
            {
                if (HasComp<BrainComponent>(id))
                {
                    brain = id;
                    _body.RemoveOrgan(brain.Value);
                    break;
                }
            }

            if (brain != null && Resolve(result, ref chassis) && chassis.BrainEntity != null)
            {
                if (_mind.TryGetMind(brain.Value, out _, out var mind) &&
                    _player.TryGetSessionById(mind.UserId, out var session) && !_borg.CanPlayerBeBorged(session))
                {
                    _itemSlots.TryInsert(chassis.BrainEntity.Value, "brain_slot", brain.Value, uid);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("borg-player-not-allowed"), brain.Value);
                }
            }
        }
    }

    private void OnSuicideByEnvironment(Entity<XenoborgFactoryComponent> entity, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        var victim = args.Victim;
        if (TryComp(victim, out ActorComponent? actor) &&
            _mind.TryGetMind(actor.PlayerSession, out var mindId, out var mind))
        {
            _ghostSystem.OnGhostAttempt(mindId, false, mind: mind);
            if (mind.OwnedEntity is { Valid: true } suicider)
            {
                _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message"), suicider);
            }
        }

        _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message-others",
                ("victim", Identity.Entity(victim, EntityManager))),
            victim,
            Filter.PvsExcept(victim, entityManager: EntityManager),
            true);

        TryStartProcessItem(entity, victim);
        args.Handled = true;
    }
}
