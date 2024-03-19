using System.Threading;
using Content.Server.Actions;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GenericAntag;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.StationEvents.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.LegallyDistinctSpaceFerret;

public sealed class LegallyDistinctSpaceFerretSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, GenericAntagCreatedEvent>(OnInit);
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, BackflipActionEvent>(OnBackflipAction);
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, EepyActionEvent>(OnEepyAction);
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, InteractionAttemptFailed>(OnInteractFailed);
        SubscribeLocalEvent<HibernateConditionComponent, ObjectiveGetProgressEvent>(OnHibernateGetProgress);
        SubscribeLocalEvent<ConsumeNutrientsConditionComponent, ObjectiveGetProgressEvent>(OnConsumeNutrientsGetProgress);
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, HungerModifiedEvent>(OnHungerModified);
    }

    private void OnInit(EntityUid uid, LegallyDistinctSpaceFerretComponent component, GenericAntagCreatedEvent args)
    {
        _actions.AddAction(uid, ref component.BackflipActionEntity, component.BackflipAction, uid);
        _actions.AddAction(uid, ref component.EepyActionEntity, component.EepyAction, uid);

        var mind = args.Mind;

        if (mind.Session == null)
            return;

        var session = mind.Session;
        _audio.PlayGlobal(new SoundPathSpecifier(component.RoleIntroSfx), Filter.Empty().AddPlayer(session), false, AudioParams.Default.WithVolume(0.66f));
        _chatMan.DispatchServerMessage(session, Loc.GetString("legallydistinctspaceferret-role-greeting"));

        _role.MindAddRole(args.MindId, new LegallyDistinctSpaceFerretRoleComponent()
        {
            PrototypeId = component.AntagProtoId
        }, mind);

        _role.MindAddRole(args.MindId, new RoleBriefingComponent
        {
            Briefing = Loc.GetString("legallydistinctspaceferret-role-briefing")
        }, mind);

        component.BrainrotEffectCanceller = new CancellationTokenSource();

        Timer.SpawnRepeating(TimeSpan.FromSeconds(1), () =>
        {
            var mobs = new HashSet<Entity<MobStateComponent>>();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, component.BrainRotAuraRadius, mobs);
            foreach (var comp in mobs)
            {
                if (HasComp<LegallyDistinctSpaceFerretComponent>(comp.Owner) || HasComp<BrainrotComponent>(comp.Owner))
                {
                    continue;
                }

                RaiseLocalEvent(new TooCloseToLDSFEvent(comp.Owner, uid, component.BrainRotAuraRadius));
            }
        }, component.BrainrotEffectCanceller.Token);
    }

    private void OnShutdown(EntityUid uid, LegallyDistinctSpaceFerretComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.BackflipActionEntity);
        _actions.RemoveAction(uid, component.EepyActionEntity);

        component.BrainrotEffectCanceller.Cancel();
    }

    public void OnInteractFailed(EntityUid uid, LegallyDistinctSpaceFerretComponent comp, InteractionAttemptFailed args)
    {
        OnBackflipAction(uid, comp, new BackflipActionEvent());
    }

    public void OnBackflipAction(EntityUid uid, LegallyDistinctSpaceFerretComponent comp, BackflipActionEvent args)
    {
        RaiseNetworkEvent(new DoABackFlipEvent(GetNetEntity(uid)));

        args.Handled = true;
    }

    public void OnEepyAction(EntityUid uid, LegallyDistinctSpaceFerretComponent comp, EepyActionEvent args)
    {
        if (_mind.TryGetObjectiveComp<ConsumeNutrientsConditionComponent>(uid, out var nutrientsCondition) && nutrientsCondition.NutrientsConsumed / nutrientsCondition.NutrientsRequired < 1.0)
        {
            _popup.PopupEntity(Loc.GetString(comp.NotEnoughNutrientsMessage), uid, PopupType.SmallCaution);

            return;
        }

        var scrubbers = _lookup.GetEntitiesInRange<GasVentScrubberComponent>(Transform(uid).Coordinates, 2f);
        if (scrubbers.Count <= 0)
        {
            _popup.PopupEntity(Loc.GetString(comp.OutOfRangeMessage), uid, PopupType.SmallCaution);

            return;
        }

        // Popup that you won!
        _popup.PopupEntity(Loc.GetString(comp.YouWinMessage), uid, PopupType.Large);

        // Play SFX for all!
        _audio.PlayPvs(new SoundPathSpecifier(comp.RoleOutroSfx), uid, AudioParams.Default.WithVolume(0.66f));

        // Green text!
        if (_mind.TryGetObjectiveComp<HibernateConditionComponent>(uid, out var obj))
        {
            obj.Hibernated = true;
        }

        var mind = _mind.GetMind(uid);
        if (mind != null)
        {
            _ticker.OnGhostAttempt(mind.Value, false);
        }

        AddComp<BlockMovementComponent>(uid);
        RemComp<ActiveNPCComponent>(uid);
        RemComp<GhostTakeoverAvailableComponent>(uid);

        RaiseNetworkEvent(new GoEepyEvent(GetNetEntity(uid)));
        args.Handled = true;
    }

    private void OnHibernateGetProgress(EntityUid uid, HibernateConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.Hibernated ? 1.0f : 0.0f;
    }

    private void OnConsumeNutrientsGetProgress(EntityUid uid, ConsumeNutrientsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.NutrientsConsumed / comp.NutrientsRequired;
    }

    private void OnHungerModified(EntityUid uid, LegallyDistinctSpaceFerretComponent comp, HungerModifiedEvent args)
    {
        if (_mind.TryGetObjectiveComp<ConsumeNutrientsConditionComponent>(uid, out var nutrientsCondition) && args.Amount > 0)
        {
            nutrientsCondition.NutrientsConsumed += args.Amount;
        }
    }
}

[RegisterComponent, Access(typeof(LegallyDistinctSpaceFerretSystem)), ExclusiveAntagonist]
public sealed partial class LegallyDistinctSpaceFerretRoleComponent : AntagonistRoleComponent
{
}

[RegisterComponent]
public sealed partial class ConsumeNutrientsConditionComponent : Component
{
    [DataField]
    public float NutrientsRequired = 150.0f;

    public float NutrientsConsumed = 0f;
}

[RegisterComponent]
public sealed partial class HibernateConditionComponent : Component
{
    public bool Hibernated;
}

[RegisterComponent]
public sealed partial class LegallyDistinctSpaceFerretSpawnRuleComponent : Component
{
}

public sealed class LegallyDistinctSpaceFerretSpawnRule : StationEventSystem<LegallyDistinctSpaceFerretSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, LegallyDistinctSpaceFerretSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        TryFindRandomTile(out _, out _, out _, out var coords);
        Sawmill.Info($"Creating ferret spawnpoint at {coords}");
        Spawn("SpawnPointGhostLegallyDistinctSpaceFerret", coords);
    }
}
