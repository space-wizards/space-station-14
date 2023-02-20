using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;

namespace Content.Server.HotPotato;

public sealed class HotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HotPotatoComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, HotPotatoComponent component, UseInHandEvent args)
    {
        EntityManager.EnsureComponent<UnremoveableComponent>(uid);
    }
}