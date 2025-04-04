using Robust.Shared.Timing;

namespace Content.Shared.Emoting;

public sealed class EmoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!; //Starlight-edit
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteAttemptEvent>(OnEmoteAttempt);
    }

    public void SetEmoting(EntityUid uid, bool value, EmotingComponent? component = null)
    {
        if (value && !Resolve(uid, ref component))
            return;

        component = EnsureComp<EmotingComponent>(uid);

        if (component.Enabled == value)
            return;

        Dirty(uid, component);
    }

    private void OnEmoteAttempt(EmoteAttemptEvent args)
    {
        if (!TryComp(args.Uid, out EmotingComponent? emote) || !emote.Enabled)
            args.Cancel();
        
        //Starlight-start
        if (emote == null)
            return;
        
        //check if they are on cooldown
        if (_gameTiming.CurTime > emote.LastEmoteTime + emote.EmoteCooldown)
        {
            emote.LastEmoteTime = _gameTiming.CurTime;
        }
        else
        {
            args.Cancel();
        }
        //Starlight-end
    }
}
