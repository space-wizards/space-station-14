using Content.Shared.Buckle;
using Content.Server.Chat.Systems;
using Content.Shared.Flip;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;

namespace Content.Server.Flip;

public sealed class FlipAnimationSystem : SharedFlipAnimationSystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public const string FlipEmoteId = "Flip";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlipAnimationComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<FlipAnimationComponent> ent, ref EmoteEvent args)
    {
        if (args.Emote.ID != FlipEmoteId)
            return;

        // Do nothing if buckled in
        if (_buckle.IsBuckled(ent.Owner))
            return;

        // Do nothing if crit or dead
        if (_mobState.IsIncapacitated(ent.Owner))
            return;

        // Do nothing if knocked down
        if (HasComp<KnockedDownComponent>(ent))
            return;

        StartFlip(ent);
    }

    public void StartFlip(Entity<FlipAnimationComponent> entity)
    {
        RaiseNetworkEvent(new StartFlipEvent(GetNetEntity(entity.Owner)));
    }

    public void StopFlip(Entity<FlipAnimationComponent> entity)
    {
        RaiseNetworkEvent(new StopFlipEvent(GetNetEntity(entity.Owner)));
    }
}
