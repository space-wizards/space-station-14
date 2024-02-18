namespace Content.Shared.Shuttles.Systems;

public abstract class SharedDockingSystem : EntitySystem
{
    public const float DockingHiglightRange = 4f;

    public bool CanDock(NetEntity dock1, NetEntity dock2)
    {
        return false;
    }
}
