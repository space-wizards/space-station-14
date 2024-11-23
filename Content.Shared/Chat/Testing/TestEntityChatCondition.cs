using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Chat.Testing;

[Serializable]
[DataDefinition]
public sealed partial class TestEntityChatCondition : EntityChatCondition
{

    public override HashSet<EntityUid> FilterConsumers(HashSet<EntityUid> consumers, EntityUid? senderEntity) { return consumers; }

}

