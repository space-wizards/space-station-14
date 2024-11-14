// Original code by whateverusername0 from Goob-Station at commit 3022db4
// Available at: https://github.com/Goob-Station/Goob-Station/blob/3022db48e89ff00b762004767e7850023df3ee97/Content.Shared/_Goobstation/Emoting/SharedAnimatedEmotesSystem.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Harmony.Emoting;

public abstract class SharedAnimatedEmotesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<AnimatedEmotesComponent> ent, ref ComponentGetState args)
    {
        args.State = new AnimatedEmotesComponentState(ent.Comp.Emote);
    }
}
