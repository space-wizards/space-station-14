using Content.Shared.Administration;
using Content.Shared.Prayer;

namespace Content.Client.Prayer;
/// <summary>
/// System to handle subtle messages and praying
/// </summary>
public sealed class PrayerSystem : SharedPrayerSystem
{
    protected override void OnPrayerTextMessage(PrayerTextMessage message, EntitySessionEventArgs eventArgs)
    {
        var bwoinkMessage = new SharedBwoinkSystem.BwoinkTextMessage(eventArgs.SenderSession.UserId,
            eventArgs.SenderSession.UserId, message.Text);

        RaiseNetworkEvent(bwoinkMessage);
    }
}
