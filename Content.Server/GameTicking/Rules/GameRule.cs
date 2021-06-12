using JetBrains.Annotations;

namespace Content.Server.GameTicking.Rules
{
    [PublicAPI]
    public abstract class GameRule
    {
        public virtual void Added()
        {

        }

        public virtual void Removed()
        {

        }
    }
}
