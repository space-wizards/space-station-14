// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead;

[Serializable, NetSerializable]
public sealed partial class RequestNecroficationEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public string Sprite { get; }
    public string State { get; }
    public bool IsAnimal { get; }
    public RequestNecroficationEvent(NetEntity netEntity, string sprite, string state, bool isAnimal)
    {
        NetEntity = netEntity;
        Sprite = sprite;
        State = state;
        IsAnimal = isAnimal;
    }
}
