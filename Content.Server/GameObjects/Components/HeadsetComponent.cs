using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class HeadsetComponent : Component
    {
        public override string Name => "Headset";

#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IServerNetManager _netManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

        }

        public void Test()
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            Console.WriteLine("Test functional.");
        }
    }
}
