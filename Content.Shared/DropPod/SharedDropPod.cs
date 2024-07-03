using Robust.Shared.Serialization;

namespace Content.Shared.DropPod;

[Serializable, NetSerializable]
public enum DropPodUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DropPodUiState : BoundUserInterfaceState
{
    public bool CanRefreshVol { get; }
    public bool CanStartVol { get; }
    public Dictionary<int, string> Points = new Dictionary<int, string>();

    public DropPodUiState(bool canRefreshVol, bool canStartVol, Dictionary<int, string> points)
    {
        CanRefreshVol = canRefreshVol;
        CanStartVol = canStartVol;
        Points.Clear();
        foreach(var point in points)
        {
            Points.Add(point.Key, point.Value);
        }
    }
}

[Serializable, NetSerializable]
public sealed class DropPodRefreshMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DropPodStartMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DropPodPointSelectedMessage : BoundUserInterfaceMessage
{
    public int Point { get; }

    public DropPodPointSelectedMessage(int point)
    {
        Point = point;
    }
}
