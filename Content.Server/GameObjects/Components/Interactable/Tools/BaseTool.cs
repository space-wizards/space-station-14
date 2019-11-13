// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    public abstract class ToolComponent : Component
    {
        /// <summary>
        /// For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedModifier
        {
            get => _speedModifier;
            set => _speedModifier = value;
        }
        private float _speedModifier = 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _speedModifier, "Speed", 1);
        }

        /// <summary>
        /// Status modifier which determines whether or not we can act as a tool at this time
        /// </summary>
        /// <returns></returns>
        public virtual bool CanUse()
        {
            return true;
        }
    }
}
