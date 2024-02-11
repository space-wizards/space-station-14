using Content.Server.GameTicking.Rules;
using Content.Server.Traitor.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.Traitor.Systems;

/// <summary>
/// Makes entities with <see cref="AutoTraitorComponent"/> a traitor either immediately if they have a mind or when a mind is added.
/// </summary>
public sealed class AutoTraitorSystem : EntitySystem
{
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoTraitorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutoTraitorComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMapInit(EntityUid uid, AutoTraitorComponent comp, MapInitEvent args)
    {
        TryMakeTraitor(uid, comp);
    }

    private void OnMindAdded(EntityUid uid, AutoTraitorComponent comp, MindAddedMessage args)
    {
        TryMakeTraitor(uid, comp);
    }

    /// <summary>
    /// Sets the GiveUplink field.
    /// </summary>
    public void SetGiveUplink(EntityUid uid, bool giveUplink, AutoTraitorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.GiveUplink = giveUplink;
    }

    /// <summary>
    /// Sets the GiveObjectives field.
    /// </summary>
    public void SetGiveObjectives(EntityUid uid, bool giveObjectives, AutoTraitorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.GiveObjectives = giveObjectives;
    }

    /// <summary>
    /// Checks if there is a mind, then makes it a traitor using the options.
    /// </summary>
    public bool TryMakeTraitor(EntityUid uid, AutoTraitorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
            return false;

        var mindId = mindContainer.Mind.Value;
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.Session == null)
            return false;

        var session = mind.Session;
        _traitorRule.MakeTraitor(session, giveUplink: comp.GiveUplink, giveObjectives: comp.GiveObjectives);
        // prevent spamming anything if it fails
        RemComp<AutoTraitorComponent>(uid);
        return true;
    }
}
