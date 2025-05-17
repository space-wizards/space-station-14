using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Transform;


[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity TargetIdentity;

    public ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity)
    {
        TargetIdentity = targetIdentity;
    }
}

[Serializable, NetSerializable]
public sealed class ChangelingIdentityData
{
    public readonly NetEntity Identity;
    public string Name;

    public ChangelingIdentityData(NetEntity identity, string name)
    {
        Identity = identity;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class ChangelingTransformBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ChangelingIdentityData> Identites;

    public ChangelingTransformBoundUserInterfaceState(List<ChangelingIdentityData> identities)
    {
        Identites = identities;
    }
}

[Serializable, NetSerializable]
public enum TransformUi : byte
{
    Key,
}
