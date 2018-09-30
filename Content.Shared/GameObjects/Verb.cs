using System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Players;

namespace Content.Shared.GameObjects
{
    public abstract class Verb
    {
        public abstract string GetName(IEntity user, IComponent component);
        public abstract bool IsDisabled(IEntity user, IComponent component);
        public abstract void Activate(IEntity user, IComponent component);
    }
}
