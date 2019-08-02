using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that creates power and supplies it to the powernet
    /// </summary>
    [RegisterComponent]
    public class PowerGeneratorComponent : Component
    {
        public override string Name => "PowerGenerator";

        /// <summary>
        /// Power supply from this entity
        /// </summary>
        private float _supply = 1000; //arbitrary initial magic number to start
        [ViewVariables]
        public float Supply
        {
            get => _supply;
            set { UpdateSupply(value); }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _supply, "supply", 1000);
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
                    node.Parent.RemoveGenerator(this);
                }

                node.OnPowernetConnect -= PowernetConnect;
                node.OnPowernetDisconnect -= PowernetDisconnect;
                node.OnPowernetRegenerate -= PowernetRegenerate;
            }

            base.OnRemove();
        }

        private void UpdateSupply(float value)
        {
            _supply = value;
            var node = Owner.GetComponent<PowerNodeComponent>();
            node.Parent.UpdateGenerator(this);
        }

        /// <summary>
        /// Node has become anchored to a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddGenerator(this);
        }

        /// <summary>
        /// Node has had its powernet regenerated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetRegenerate(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddGenerator(this);
        }

        /// <summary>
        /// Node has become unanchored from a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemoveGenerator(this);
        }
    }
}
