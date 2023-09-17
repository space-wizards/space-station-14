using Content.Shared.Interaction.Events;
using Content.Shared.Whistle.Components;
using Content.Shared.Whistle.Events;

namespace Content.Server.Whistle;

public sealed class WhistleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhistleComponent, UseInHandEvent>(OnUseInHand);
    }

    public void OnUseInHand(EntityUid uid, WhistleComponent component, UseInHandEvent args)
    {
        RaiseNetworkEvent(new OnWhistleEvent(uid, args.User));
    }
}
