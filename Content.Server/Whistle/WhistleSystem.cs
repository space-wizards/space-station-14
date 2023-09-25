using Content.Shared.Interaction.Events;
using Content.Shared.Whistle;

namespace Content.Server.Whistle;

public sealed class WhistleSystem : SharedWhistleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhistleComponent, UseInHandEvent>(OnUseInHand);
    }
}
