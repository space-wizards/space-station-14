using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Rules
{
    [PublicAPI]
    public abstract class GameRule : IEntityEventSubscriber
    {
        public virtual void Added()
        {

        }

        public virtual void Removed()
        {

        }
    }
}
