using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Restraints
{
    public class SharedHandcuffComponent : Component
    {
        private TimeSpan _breakoutTime;
        public override string Name => "Restrained";
        public override uint? NetID => ContentNetIDs.HANDCUFFS;

        /// <summary>
        ///    How long it takes to break out of handcuffs
        /// </summary>
        public TimeSpan BreakoutTime => _breakoutTime;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField<TimeSpan>(ref _breakoutTime, "breakouttime", TimeSpan.FromSeconds(10));
        }

    }
}
