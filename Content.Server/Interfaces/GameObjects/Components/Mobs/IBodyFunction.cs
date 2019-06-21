using Robust.Shared.Interfaces.GameObjects;
using Content.Server.GameObjects.Components.Mobs.Body;

namespace Content.Server.Interfaces.GameObjects.Components.Mobs
{
    public interface IBodyFunction
    {
        OrganNode Node { get; }
        void Life(IEntity onEntity, OrganState state);
        void OnStateChange(IEntity onEntity, OrganState state);
    }
}
