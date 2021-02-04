#nullable enable
using Content.Server.Administration;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerComponent))]
    [ComponentReference(typeof(ISolutionInteractionsComponent))]
    public class SolutionContainerComponent : SharedSolutionContainerComponent
    {
    }
}
