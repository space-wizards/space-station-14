using Robust.Shared.Serialization;

namespace Content.Shared.Prayer;

/// <summary>
/// Shared system for handling Prayers
/// </summary>
public abstract class SharedPrayerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<PrayerTextMessage>(OnPrayerTextMessage);
    }

    protected virtual void OnPrayerTextMessage(PrayerTextMessage message, EntitySessionEventArgs eventArgs)
    {
        // Specific side code in target.
    }

    [Serializable, NetSerializable]
    public sealed class PrayerTextMessage : EntityEventArgs
    {
        public string Text { get; }

        public PrayerTextMessage(string text)
        {
            Text = text;
        }
    }
}
