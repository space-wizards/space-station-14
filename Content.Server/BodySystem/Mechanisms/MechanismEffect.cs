




using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Robust.Shared.BodySystem {
    /// <summary>
    ///     Interface for a mechanism effect.
    /// </summary>			  
    public interface IMechanismEffect : IExposeData {
		public void Tick(IEntity ParentEntity);
	}
}
