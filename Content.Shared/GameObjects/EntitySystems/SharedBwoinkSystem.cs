#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.EntitySystemMessages;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedBwoinkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BwoinkSystemMessages.BwoinkTextMessage>(OnBwoinkTextMessage);
        }

        protected virtual void OnBwoinkTextMessage(BwoinkSystemMessages.BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            // Specific side code in target.
        }

        protected void LogBwoink(BwoinkSystemMessages.BwoinkTextMessage message)
        {
            Logger.InfoS("c.s.go.es.bwoink", $"@{message.ChannelId}: {message.Text}");
        }
    }
}
