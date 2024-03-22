using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GenericAntag;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.StationEvents.Events;
using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Mind;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.LegallyDistinctSpaceFerret;

public sealed class LegallyDistinctSpaceFerretSystem : EntitySystem
{
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
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, InteractionAttemptFailed>(OnInteractFailed);
        SubscribeLocalEvent<LegallyDistinctSpaceFerretComponent, HungerModifiedEvent>(OnHungerModified);
    }

    private void OnInit(EntityUid uid, LegallyDistinctSpaceFerretComponent component, GenericAntagCreatedEvent args)
    {
        var mind = args.Mind;

        if (mind.Session == null)
            return;

        var session = mind.Session;
        _role.MindAddRole(args.MindId, new RoleBriefingComponent
        {
            Briefing = Loc.GetString(component.RoleBriefing)
        }, mind);
        _role.MindAddRole(args.MindId, new LegallyDistinctSpaceFerretRoleComponent()
        {
            PrototypeId = component.AntagProtoId
        }, mind);
        _role.MindPlaySound(args.MindId, new SoundPathSpecifier(component.RoleIntroSfx), mind);
        _chatMan.DispatchServerMessage(session, Loc.GetString(component.RoleGreeting));
    }

    public void OnInteractFailed(EntityUid uid, LegallyDistinctSpaceFerretComponent _, InteractionAttemptFailed args)
    {
        RaiseLocalEvent(uid, new BackflipActionEvent());
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
public sealed partial class LegallyDistinctSpaceFerretRoleComponent : AntagonistRoleComponent;

[RegisterComponent]
public sealed partial class LegallyDistinctSpaceFerretSpawnRuleComponent : Component;

public sealed class LegallyDistinctSpaceFerretSpawnRule : StationEventSystem<LegallyDistinctSpaceFerretSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, LegallyDistinctSpaceFerretSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        TryFindRandomTile(out _, out _, out _, out var coords);
        Sawmill.Info($"Creating ferret spawn point at {coords}");
        Spawn("SpawnPointGhostLegallyDistinctSpaceFerret", coords);
    }
}
