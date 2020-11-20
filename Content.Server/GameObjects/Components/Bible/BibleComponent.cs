using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Bible;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.Bible
{
    [RegisterComponent]
    internal class BibleComponent : SharedBibleComponent, IUse
    {
        private BoundUserInterface UserInterface => Owner.GetUIOrNull(BibleUiKey.Key);

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _styleSelected;

        [ViewVariables]
        private List<string> _styles;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _styleSelected, "styleSelected", true);
            serializer.DataField(ref _styles, "styles", new List<string>());
        }

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            UserInterface.OnReceiveMessage -= UserInterfaceOnReceiveMessage;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case BibleSelectStyleMessage msg:
                    {
                        if (Owner.TryGetComponent(out AppearanceComponent appearance))
                        {
                            appearance.SetData(BibleVisuals.Style, msg.Style);
                        }
                        _styleSelected = true;
                        UserInterface.CloseAll();
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (_styleSelected)
                return false;

            var session = eventArgs.User.PlayerSession();
            if (session != null)
                ToggleUI(session);

            return true;
        }

        private void ToggleUI(IPlayerSession session)
        {
            UserInterface.Toggle(session);
            if (UserInterface.SessionHasOpen(session))
            {
                UserInterface.SetState(new BibleBoundUserInterfaceState(_styles));
            }
        }
    }
}
