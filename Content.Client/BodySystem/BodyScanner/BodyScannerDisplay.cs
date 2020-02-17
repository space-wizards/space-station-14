using Content.Client.BodySystem;
using Content.Shared.BodySystem;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Client.UserInterface
{
    public sealed class BodyScannerDisplay : SS14Window
    {
        #pragma warning disable 649
                [Dependency] private readonly ILocalizationManager _loc;
        #pragma warning restore 649

        protected override Vector2? CustomSize => (400, 600);

        public BodyScannerBoundUserInterface Owner { get; private set; }

        public BodyScannerDisplay(BodyScannerBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;
            Title = _loc.GetString("Body Scanner");
        }

        public void UpdateDisplay(BodyTemplatePrototype _template, Dictionary<string, BodyPart> _parts)
        {
            if (_template != null)
                Title = _template.Name;
            else
                Title = "ATE#INJM#OI$N#JKO$PNB$";
        }
    }
}
