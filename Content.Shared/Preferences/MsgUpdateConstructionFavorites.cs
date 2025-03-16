using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to update their construction favorites.
    /// </summary>
    public sealed class MsgUpdateConstructionFavorites : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public IReadOnlyList<string> Favorites = [];

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var length = buffer.ReadVariableInt32();
            var favorites = new string[length];
            for (var i = 0; i < length; i++)
            {
                favorites[i] = buffer.ReadString();
            }
            Favorites = favorites;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.WriteVariableInt32(Favorites.Count);
            foreach (var favorite in Favorites)
            {
                buffer.Write(favorite);
            }
        }
    }
}
