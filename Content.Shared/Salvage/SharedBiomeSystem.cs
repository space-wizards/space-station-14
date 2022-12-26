using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Salvage;

public abstract class SharedBiomeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, ComponentGetState>(OnBiomeGetState);
        SubscribeLocalEvent<BiomeComponent, ComponentHandleState>(OnBiomeHandleState);
    }

    private void OnBiomeHandleState(EntityUid uid, BiomeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BiomeComponentState state)
            return;

        component.Seed = state.Seed;
        component.Prototype = state.Prototype;
    }

    private void OnBiomeGetState(EntityUid uid, BiomeComponent component, ref ComponentGetState args)
    {
        args.State = new BiomeComponentState(component.Seed, component.Prototype);
    }

    [Serializable, NetSerializable]
    private sealed class BiomeComponentState : ComponentState
    {
        public int Seed;
        public string Prototype;

        public BiomeComponentState(int seed, string prototype)
        {
            Seed = seed;
            Prototype = prototype;
        }
    }
}
