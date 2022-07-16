using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Parallax;

/// <summary>
/// Handles per-map parallax in sim. Out of sim parallax is handled by ParallaxManager.
/// </summary>
public abstract class SharedParallaxSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParallaxComponent, ComponentGetState>(OnParallaxGetState);
        SubscribeLocalEvent<ParallaxComponent, ComponentHandleState>(OnParallaxHandleState);
    }

    private void OnParallaxGetState(EntityUid uid, ParallaxComponent component, ref ComponentGetState args)
    {
        args.State = new ParallaxComponentState
        {
            Parallax = component.Parallax
        };
    }

    private void OnParallaxHandleState(EntityUid uid, ParallaxComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ParallaxComponentState state) return;
        component.Parallax = state.Parallax;
    }

    [Serializable, NetSerializable]
    private sealed class ParallaxComponentState : ComponentState
    {
        public string Parallax = string.Empty;
    }
}
