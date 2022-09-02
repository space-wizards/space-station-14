using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    public abstract class SharedPrayerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PrayTextMessage>(OnPrayTextMessage);
        }
        protected virtual void OnPrayTextMessage(PrayTextMessage message, EntitySessionEventArgs eventArgs)
        {
            // Specific side code in target.
        }

        [Serializable, NetSerializable]
        public sealed class PrayTextMessage : EntityEventArgs
        {
            public string Text { get; }

            public PrayTextMessage(string text)
            {
                Text = text;
            }
        }
    }
}

