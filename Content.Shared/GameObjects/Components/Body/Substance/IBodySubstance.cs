using Content.Shared.GameObjects.Components.Body.Conduit;

namespace Content.Shared.GameObjects.Components.Body.Substance
{
    /// <summary>
    ///     Represents a substance within the body.
    /// </summary>
    public interface IBodySubstance
    {
        /// <summary>
        ///     The biggest type of this substance.
        /// </summary>
        BodySubstanceType Type { get; }
    }
}
