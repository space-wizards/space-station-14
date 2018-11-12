using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Players;

namespace Content.Shared.GameObjects
{
    public interface IVerbProvider
    {
        IEnumerable<Verb> GetVerbs(IEntity userEntity);
    }

    public interface IVerbProviderComponent : IComponent, IVerbProvider
    {
    }
}
