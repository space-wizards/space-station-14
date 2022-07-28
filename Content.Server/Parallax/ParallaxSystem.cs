using Content.Shared.Parallax;
using Robust.Shared.GameStates;

namespace Content.Server.Parallax;

public sealed class ParallaxSystem : SharedParallaxSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParallaxComponent, ComponentGetState>(OnParallaxGetState);
    }

    private void OnParallaxGetState(EntityUid uid, ParallaxComponent component, ref ComponentGetState args)
    {
        args.State = new ParallaxComponentState
        {
            Parallax = component.Parallax
        };
    }

}
