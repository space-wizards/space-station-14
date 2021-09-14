using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Client.MobState
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMobStateComponent))]
    [ComponentReference(typeof(IMobStateComponent))]
    public class MobStateComponent : SharedMobStateComponent
    {
    }
}
