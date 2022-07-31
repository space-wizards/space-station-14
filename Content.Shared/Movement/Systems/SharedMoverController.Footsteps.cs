using Content.Shared.Movement.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeFootsteps()
    {
        SubscribeLocalEvent<FootstepModifierComponent, ComponentGetState>(OnFootGetState);
        SubscribeLocalEvent<FootstepModifierComponent, ComponentHandleState>(OnFootHandleState);
    }

    private void OnFootHandleState(EntityUid uid, FootstepModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FootstepModifierComponentState state) return;
        component.Sound = state.Sound;
    }

    private void OnFootGetState(EntityUid uid, FootstepModifierComponent component, ref ComponentGetState args)
    {
        args.State = new FootstepModifierComponentState()
        {
            Sound = component.Sound,
        };
    }

    [Serializable, NetSerializable]
    private sealed class FootstepModifierComponentState : ComponentState
    {
        public SoundSpecifier Sound = default!;
    }
}
