using Content.Server.Antag;
using Content.Server.Traitor.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Systems;

/// <summary>
/// Makes entities with <see cref="AutoTraitorComponent"/> a traitor either immediately if they have a mind or when a mind is added.
/// </summary>
public sealed class AutoTraitorSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultTraitorRule = "Traitor";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoTraitorComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, AutoTraitorComponent comp, MindAddedMessage args)
    {
        _antag.ForceMakeAntag<AutoTraitorComponent>(args.Mind.Comp.Session, DefaultTraitorRule);
    }
}
