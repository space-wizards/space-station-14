using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;

    public event Action<CharacterData>? OnCharacterUpdate;
    public event Action? OnCharacterDetached;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);

        SubscribeNetworkEvent<CharacterInfoEvent>(OnCharacterInfoEvent);
    }

    public void RequestCharacterInfo()
    {
        var entity = _players.LocalPlayer?.ControlledEntity;
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestCharacterInfoEvent(entity.Value));
    }

    private void OnPlayerAttached(PlayerAttachSysMessage msg)
    {
        if (msg.AttachedEntity == default)
        {
            OnCharacterDetached?.Invoke();
        }
    }

    private void OnCharacterInfoEvent(CharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        var sprite = CompOrNull<SpriteComponent>(msg.EntityUid);
        var data = new CharacterData(msg.JobTitle, msg.Objectives, msg.Briefing, sprite, Name(msg.EntityUid));

        OnCharacterUpdate?.Invoke(data);
    }

    public readonly record struct CharacterData(
        string Job,
        Dictionary<string, List<ConditionInfo>> Objectives,
        string Briefing,
        SpriteComponent? Sprite,
        string EntityName
    );
}
