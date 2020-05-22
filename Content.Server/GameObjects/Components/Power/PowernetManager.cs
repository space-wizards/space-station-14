namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Responsible for managing an aspect of a <see cref="Wirenet"/>.
    /// For example, <see cref="PowernetPowerManager"/> manages the power network.
    /// PowernetManagers are like components, but they're not directly connected to the world.
    /// They are also like EntitySystems, in that they are automatically created when possible.
    /// The expected constructor is the one provided by this class.
    /// (An important side note is that PowernetManagers aren't aware of when nodes are added.
    /// The node components have to manage this themselves via OnAdd/OnRemove,
    ///  along with some events of PowerNodeComponent.
    /// This likely has something to do with ensuring the runtime addition/removal of
    /// components doesn't break, so I'm not modifying this part of the design. - 20kdc)
    /// </summary>
    public abstract class PowernetManager
    {
        /// <summary>
        /// The Powernet this PowernetManager is responsible for.
        /// </summary>
        public readonly Powernet powernet;

        public PowernetManager(Powernet net)
        {
            powernet = net;
        }

        /// <summary>
        /// Called after all managers are connected to the powernet.
        /// The network is guaranteed to be empty at this time.
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Called to update time-continuous activity (the usual reason for this to exist).
        /// </summary>
        public virtual void Update(float frameTime)
        {

        }

        /// <summary>
        /// Called to merge all of the devices from another powernet.
        /// This is also responsible for cleaning up the correspondent manager
        ///  in the other powernet.
        /// </summary>
        public virtual void MergeFrom(Powernet from)
        {

        }

        /// <summary>
        /// Called when the wirenet is being shutdown due to a wiring regeneration.
        /// Every wire and node involved in the Wirenet has gone somewhere else.
        /// *You will not have received any of the disconnection events.*
        /// </summary>
        public virtual void DirtyKill()
        {

        }
    }
}
