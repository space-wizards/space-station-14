using Content.Client.Chat.Managers;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Client.Player;
using Robust.Shared.Map;

namespace Content.Client.Chat;

public sealed class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IChatManager _manager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ExamineSystem _examineSystem = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _player.LocalPlayer?.ControlledEntity;
        var predicate = static (EntityUid uid, (EntityUid compOwner, EntityUid? attachedEntity) data)
            => uid == data.compOwner || uid == data.attachedEntity;
        var bubbles = _manager.GetSpeechBubbles();
        var playerPos = player != null ? Transform(player.Value).MapPosition : MapCoordinates.Nullspace;

        var occluded = player != null && _examineSystem.IsOccluded(player.Value);

        foreach (var (ent, bubs) in bubbles)
        {
            if (Deleted(ent))
            {
                SetBubbles(bubs, false);
                continue;
            }

            if (ent == player)
            {
                SetBubbles(bubs, true);
                continue;
            }

            var otherPos = Transform(ent).MapPosition;

            if (occluded && !ExamineSystemShared.InRangeUnOccluded(
                    playerPos,
                    otherPos, 0f,
                    (ent, player), predicate))
            {
                SetBubbles(bubs, false);
                continue;
            }

            SetBubbles(bubs, true);
        }
    }

    private void SetBubbles(List<SpeechBubble> bubbles, bool value)
    {
        foreach (var bubble in bubbles)
        {
            bubble.Visible = value;
        }
    }
}
