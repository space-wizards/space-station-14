using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

/// <summary>
/// Mopping logic for interacting with puddle components.
/// </summary>
public abstract class SharedAbsorbentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, ComponentGetState>(OnAbsorbentGetState);
        SubscribeLocalEvent<AbsorbentComponent, ComponentHandleState>(OnAbsorbentHandleState);
    }

    private void OnAbsorbentHandleState(EntityUid uid, AbsorbentComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AbsorbentComponentState state)
            return;

        if (component.Progress.OrderBy(x => x.Key.ToArgb()).SequenceEqual(state.Progress))
            return;

        component.Progress.Clear();
        foreach (var item in state.Progress)
        {
            component.Progress.Add(item.Key, item.Value);
        }
    }

    private void OnAbsorbentGetState(EntityUid uid, AbsorbentComponent component, ref ComponentGetState args)
    {
        args.State = new AbsorbentComponentState(component.Progress);
    }

    [Serializable, NetSerializable]
    protected sealed class AbsorbentComponentState : ComponentState
    {
        public Dictionary<Color, float> Progress;

        public AbsorbentComponentState(Dictionary<Color, float> progress)
        {
            Progress = progress;
        }
    }
}
