using Content.Server.GameTicking.Rules;
using Content.Server.Traitor.Components;
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

        SubscribeLocalEvent<AutoTraitorComponent, MindAddedMessage>(OnMindAdded);
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

        //Start the rule if it has not already been started
        var traitorRuleComponent = _traitorRule.StartGameRule();
        _traitorRule.MakeTraitor(uid, traitorRuleComponent, giveUplink: comp.GiveUplink, giveObjectives: comp.GiveObjectives);
        // prevent spamming anything if it fails
        RemComp<AutoTraitorComponent>(uid);
        return true;
    }
}
