using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Interaction.Events;

namespace Content.Server.GreyStation.NPC;

public sealed class HostileOnInteractFailSystem : EntitySystem
{
    [Dependency] private readonly NPCRetaliationSystem _retaliation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HostileOnInteractFailComponent, InteractionFailureEvent>(OnInteractionFailed);
    }

    private void OnInteractionFailed(Entity<HostileOnInteractFailComponent> ent, ref InteractionFailureEvent args)
    {
        if (TryComp<NPCRetaliationComponent>(ent, out var retaliation))
            _retaliation.TryRetaliate((ent, retaliation), args.User);
    }
}
