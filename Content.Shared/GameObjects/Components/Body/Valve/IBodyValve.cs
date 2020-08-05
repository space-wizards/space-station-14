#nullable enable
using Content.Shared.GameObjects.Components.Body.Conduit;
using Content.Shared.GameObjects.Components.Body.Connector;

namespace Content.Shared.GameObjects.Components.Body.Valve
{
    /// <summary>
    ///     Represents a valve within the body which permits the movement of substances in
    ///     one direction only.
    /// </summary>
    public interface IBodyValve
    {
        IBodyConnector? First { get; set; }
        
        IBodyConnector? Second { get; set; }

        /// <summary>
        ///     The two connections that this valve sits between.
        /// </summary>
        (IBodyConnector?, IBodyConnector?) Connections => (First, Second);

        /// <summary>
        ///     The connector that this valve opens towards.
        /// </summary>
        IBodyConnector OpensTowards { get; }

        /// <summary>
        ///     Whether or not this valve is currently active.
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        ///     The pressure that this valve is currently withstanding.
        /// </summary>
        int Pressure { get; set; }

        /// <summary>
        ///     The maximum pressure that this valve can sustain before forcefully opening.
        /// </summary>
        int MaxPressure { get; set; }

        /// <summary>
        ///     Checks if a substance can be pushed to a connector with a certain pressure.
        /// </summary>
        /// <param name="substance">The type of substance to push through.</param>
        /// <param name="pressure">The pressure with which to push it.</param>
        /// <param name="towards">The connector to push it towards.</param>
        /// <returns>True if it can be pushed through, false otherwise.</returns>
        bool CanPush(BodySubstanceType substance, int pressure, IBodyConnector towards);

        /// <summary>
        ///     Opens this valve.
        /// </summary>
        void Open();

        /// <summary>
        ///     Closes this valve.
        /// </summary>
        void Close();
    }
}
