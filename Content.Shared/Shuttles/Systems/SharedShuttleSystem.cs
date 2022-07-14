namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem : EntitySystem
{

}

[Flags]
public enum FTLState : byte
{
    Invalid = 0,
    Available = 1 << 0,
    Starting = 1 << 1,
    Travelling = 1 << 2,
    Cooldown = 1 << 3,
}

