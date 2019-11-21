using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    /// <summary>
    /// Tool used to weld metal together, light things on fire, or melt into constituent parts
    /// </summary>
    [RegisterComponent]
    class WelderComponent : ToolComponent, IUse, IExamine
    {
        public override string Name => "Welder";
        public string WelderFuelReagentName = "chem.WelderFuel";
        public bool Activated = false;
       
        public override void Initialize()
        {
            base.Initialize();
         
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }


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

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            //Handled in WelderSystem
            return false;
        }
    }
}
