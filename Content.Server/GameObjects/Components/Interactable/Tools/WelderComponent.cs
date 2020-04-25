using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool used to weld metal together, light things on fire, or melt into constituent parts
    /// </summary>
    [RegisterComponent]
    public class WelderComponent : ToolComponent, IExamine
    {
        public override string Name => "Welder";
        public override uint? NetID => ContentNetIDs.WELDER;
        void IExamine.Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            if (Activated)
            {
                message.AddMarkup(loc.GetString("[color=orange]Lit[/color]\n"));
            }
            else
            {
                message.AddText(loc.GetString("Not lit\n"));
            }
        }
    }
}
