using Content.Server.GameTicking.Rules;
using Content.Server.Thief.Systems;
using Content.Server.Thief.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.Thief.Systems;

/// <summary>
/// Makes entities with <see cref="AutoThiefComponent"/> a thief either immediately if they have a mind or when a mind is added.
/// </summary>
public sealed class AutoThiefSystem : EntitySystem
{
    [Dependency] private readonly ThiefSystem _thief = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoThiefComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutoThiefComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<AutoThiefComponent> autoThief, ref MindAddedMessage args)
    {
        TryMakeThief(autoThief);
    }

    private void OnMapInit(Entity<AutoThiefComponent> autoThief, ref MapInitEvent args)
    {
        TryMakeThief(autoThief);
    }

    public bool TryMakeThief(Entity<AutoThiefComponent> autoThief)
    {
        if (!TryComp<MindContainerComponent>(autoThief.Owner, out var mindContainer) || mindContainer.Mind == null)
            return false;

        var mindId = mindContainer.Mind.Value;
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.Session == null)
            return false;

        var session = mind.Session;
        //_thief.MakeThief( component settings to making thief here);
        Log.Debug(ToPrettyString(autoThief) + "TO DO: becoming a thief?");
        // prevent spamming anything if it fails (AutoTraitorSystem comment copy)
        RemComp<AutoThiefComponent>(autoThief);
        return true;
    }
}
