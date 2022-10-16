#nullable enable
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Client.Administration.Systems
{
    [UsedImplicitly]
    public sealed class BwoinkSystem : SharedBwoinkSystem
    {
        public event EventHandler<BwoinkTextMessage>? OnBwoinkTextMessageRecieved;

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            OnBwoinkTextMessageRecieved?.Invoke(this, message);
        }

        public void Send(NetUserId channelId, string text)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, channelId, text));
        }
    }
}
