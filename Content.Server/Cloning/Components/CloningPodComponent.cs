using System;
using Content.Server.Climbing;
using Content.Server.EUI;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Cloning;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Content.Shared.Preferences;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : SharedCloningPodComponent
    {
        [ViewVariables] public ContainerSlot BodyContainer = default!;
        [ViewVariables] public Mind.Mind? CapturedMind;
        [ViewVariables] public float CloningProgress = 0;
        [DataField("cloningTime")]
        [ViewVariables] public float CloningTime = 30f;
        // Used to prevent as many duplicate UI messages as possible
        [ViewVariables] public bool UiKnownPowerState = false;

        [ViewVariables]
        public CloningPodStatus Status;

        protected override void Initialize()
        {
            base.Initialize();

            BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-bodyContainer");

            //TODO: write this so that it checks for a change in power events for GORE POD cases
            // EntitySystem.Get<CloningSystem>().UpdateUserInterface(this);
        }
    }
}
