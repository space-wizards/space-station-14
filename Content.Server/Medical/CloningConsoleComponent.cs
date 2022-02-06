using System;
using Content.Server.Climbing;
using Content.Server.Cloning;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Server.Preferences.Managers;
using Content.Server.UserInterface;
using Content.Shared.Acts;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.CloningConsole;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using Content.Server.Medical.GeneticScanner;
using Content.Server.Cloning.Components;
using System.Collections.Generic;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCloningConsoleComponent))]
    public class CloningConsoleComponent : SharedCloningConsoleComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;

        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CloningConsoleUiKey.Key);
        public GeneticScannerComponent? GeneticScanner = null;
        public CloningPodComponent? CloningPod = null;
        public List<String> CloningHistory = new List<string>();
    }
}
