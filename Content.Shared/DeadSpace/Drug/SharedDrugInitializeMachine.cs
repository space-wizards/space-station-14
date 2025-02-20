using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Drug;

[Serializable, NetSerializable]
public enum DrugInitializeMachineVisualState
{
    Idle,
    Running
}

public enum DrugInitializeMachineVisualizerLayers : byte
{
    Base,
    BaseUnlit
}

