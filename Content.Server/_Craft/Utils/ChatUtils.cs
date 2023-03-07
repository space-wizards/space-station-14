using Content.Server.Chat.Systems;

namespace Content.Server._Craft.Utils;

public static class ChatUtils
{
    public static void SendMessageFromCentcom(ChatSystem chatSystem, string message, EntityUid? stationId)
    {
        if (stationId == null)
        {
            chatSystem.DispatchGlobalAnnouncement(
                message: message,
                sender: "Центральное командование",
                playSound: true,
                colorOverride: Color.Yellow
            );

            return;
        }

        chatSystem.DispatchStationAnnouncement(
            source: (EntityUid) stationId,
            message: message,
            sender: "Центральное командование",
            playDefaultSound: true,
            colorOverride: Color.Yellow
        );
    }

    public static void SendLocMessageFromCentcom(ChatSystem chatSystem, string locCode, EntityUid? stationId)
    {
        var message = Loc.GetString(locCode);
        if (message == null)
        {
            return;
        }

        SendMessageFromCentcom(chatSystem, (string) message, stationId);
    }
}
