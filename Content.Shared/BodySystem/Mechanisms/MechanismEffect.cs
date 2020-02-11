




using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.BodySystem {
    /// <summary>
    ///     Interface for a mechanism effect.
    /// </summary>			  
    public interface IMechanismEffect : IExposeData {
		public void Tick(IEntity ParentEntity);
	}
}
