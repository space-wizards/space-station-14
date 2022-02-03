using System;
using Content.Server.Preferences.Managers;
using Content.Shared.DragDrop;
using Content.Shared.GeneticScanner;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Medical.GeneticScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGeneticScannerComponent))]
    public class GeneticScannerComponent : SharedGeneticScannerComponent
    {
        public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastInternalOpenAttempt;

        public ContainerSlot _bodyContainer = default!;

        [ViewVariables]

        public bool IsOccupied => _bodyContainer.ContainedEntity != null;

        public Boolean IsOpen = true;

        protected override void Initialize()
        {
            base.Initialize();

            _bodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-bodyContainer");
        }


        // private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        // {
        //     if (obj.Message is not UiButtonPressedMessage message || obj.Session.AttachedEntity == null) return;

        //     switch (message.Button)
        //     {
        //         case UiButton.ScanDNA:
        //             if (_bodyContainer.ContainedEntity != null)
        //             {
        //                 var cloningSystem = EntitySystem.Get<CloningSystem>();

        //                 if (!_entMan.TryGetComponent(_bodyContainer.ContainedEntity.Value, out MindComponent? mindComp) || mindComp.Mind == null)
        //                 {
        //                     obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-no-soul"));
        //                     break;
        //                 }

        //                 // Null suppression based on above check. Yes, it's explicitly needed
        //                 var mind = mindComp.Mind!;

        //                 // We need the HumanoidCharacterProfile
        //                 // TODO: Move this further 'outwards' into a DNAComponent or somesuch.
        //                 // Ideally this ends with GameTicker & CloningSystem handing DNA to a function that sets up a body for that DNA.
        //                 var mindUser = mind.UserId;

        //                 if (mindUser.HasValue == false || mind.Session == null)
        //                 {
        //                     // For now assume this means soul departed
        //                     obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-soul-broken"));
        //                     break;
        //                 }

        //                 var profile = GetPlayerProfileAsync(mindUser.Value);
        //                 cloningSystem.AddToDnaScans(new ClonerDNAEntry(mind, profile));
        //             }

        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }



        // ECS this out!, when DragDropSystem and InteractionSystem refactored
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }


        // public override bool DragDropOn(DragDropEvent eventArgs)
        // {
        //     Logger.Debug("RAN COMPONENT DRAG ON");
        //     _bodyContainer.Insert(eventArgs.Dragged);
        //     return true;
        // }
    }
}
