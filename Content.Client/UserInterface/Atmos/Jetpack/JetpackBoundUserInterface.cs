using Content.Shared.GameObjects.Components.Atmos.Jetpack;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.UserInterface.Atmos.Jetpack
{
    [UsedImplicitly]
    public class JetpackBoundUserInterface : BoundUserInterface
    {
        public JetpackBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        public void ToggleJetpack()
        {
            SendMessage(new JetpackToggleMessage());
        }
    }
}
