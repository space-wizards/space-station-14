using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Research.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public sealed class ResearchPointSourceComponent : ResearchClientComponent
    {
        [DataField("pointspersecond")]
        private int _pointsPerSecond;
        [DataField("active")]
        private bool _active;
        private ApcPowerReceiverComponent? _powerReceiver;

        [ViewVariables(VVAccess.ReadWrite)]
        public int PointsPerSecond
        {
            get => _pointsPerSecond;
            set => _pointsPerSecond = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        /// <summary>
        /// Whether this can be used to produce research points.
        /// </summary>
        /// <remarks>If no <see cref="ApcPowerReceiverComponent"/> is found, it's assumed power is not required.</remarks>
        [ViewVariables]
        public bool CanProduce => Active && (_powerReceiver is null || _powerReceiver.Powered);

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out _powerReceiver);
        }
    }
}
