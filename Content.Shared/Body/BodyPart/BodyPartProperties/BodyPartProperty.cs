using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.BodyPart.BodyPartProperties
{
    /// <summary>
    ///     Property attachable to a <see cref="BodyPart" />. For example, this is used to define the speed capabilities of a
    ///     leg. The movement system will look for a LegProperty on all BodyParts.
    /// </summary>
    public abstract class BodyPartProperty : IExposeData
    {
        /// <summary>
        ///     Whether this property is currently active.
        /// </summary>
        public bool Active;

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Active, "active", true);
        }
    }
}
