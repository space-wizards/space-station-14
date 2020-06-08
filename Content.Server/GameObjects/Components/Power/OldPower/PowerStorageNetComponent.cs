using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Feeds energy from the powernet and may have the ability to supply back into it
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(PowerStorageComponent))]
    public class PowerStorageNetComponent : PowerStorageComponent
    {
        public override string Name => "PowerStorage";

        private bool _chargePowernet = false;

        /// <summary>
        /// Do we distribute power into the powernet from our stores if the powernet requires it?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ChargePowernet
        {
            get => _chargePowernet;
            set
            {
                _chargePowernet = value;
                if (Owner.TryGetComponent(out PowerNodeComponent node))
                {
                    node.Parent?.UpdateStorageType(this);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _chargePowernet, "chargepowernet", false);
        }

        public override void OnAdd()
        {
            base.OnAdd();

            if (!Owner.TryGetComponent(out PowerNodeComponent node))
            {
                Owner.AddComponent<PowerNodeComponent>();
                node = Owner.GetComponent<PowerNodeComponent>();
            }
            node.OnPowernetConnect += PowernetConnect;
            node.OnPowernetDisconnect += PowernetDisconnect;
            node.OnPowernetRegenerate += PowernetRegenerate;
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerNodeComponent node))
            {
                if (node.Parent != null)
                {
                    node.Parent.RemovePowerStorage(this);
                }

                node.OnPowernetConnect -= PowernetConnect;
                node.OnPowernetDisconnect -= PowernetDisconnect;
                node.OnPowernetRegenerate -= PowernetRegenerate;
            }

            base.OnRemove();
        }

        /// <summary>
        /// Node has become anchored to a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddPowerStorage(this);
        }

        /// <summary>
        /// Node has had its powernet regenerated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetRegenerate(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddPowerStorage(this);
        }

        /// <summary>
        /// Node has become unanchored from a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemovePowerStorage(this);
        }
    }
}
