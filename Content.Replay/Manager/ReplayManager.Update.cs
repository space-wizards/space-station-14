using System.Linq;
using Content.Client.RoundEnd;
using Content.Client.UserInterface.Systems.Chat;
using Content.Replay.Observer;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network.Messages;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Administration.SharedNetworkResourceManager;
using static Content.Shared.Projectiles.SharedProjectileSystem;
using static Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;
using static Robust.Shared.Replays.ReplayMessage;

namespace Content.Replay.Manager;

// This partial class has code for performing tick updates (effectively the actual playback part of replays).
public sealed partial class ReplayManager
{
    private void TickUpdate(FrameEventArgs args)
    {
        if (CurrentReplay == null)
        {
            StopReplay();
            return;
        }

        if (ActivelyScrubbing)
            SetIndex(ScrubbingIndex, false);

        if (CurrentReplay.CurrentIndex + 1 >= CurrentReplay.States.Count)
            Playing = false;

        _gameState.ResetPredictedEntities();

        if (Playing)
            CurrentReplay.CurrentIndex++;

        _timing.LastRealTick = _timing.LastProcessedTick = _timing.CurTick = CurrentReplay.CurTick;

        if (Playing)
        {
            var state = CurrentReplay.CurState;
            _gameState.UpdateFullRep(state, cloneDelta: true);
            _gameState.ApplyGameState(state, CurrentReplay.NextState);
            DebugTools.Assert(CurrentReplay.LastApplied + 1 == state.ToSequence);
            CurrentReplay.LastApplied = state.ToSequence;
            ProcessMessages(CurrentReplay.CurMessages, false);
        }

        _timing.CurTick += 1;
        _entMan.TickUpdate(args.DeltaSeconds, noPredictions: true);

        // TODO REPLAYS fix this shit.
        // This is extremely hacky. Seeing as the client isn't actually running physics/predictions while playing back a
        // replay (noPredictions: true), but we still need the observer/ghost to move somehow.
        // Maybe I should just add custom mover code that is specific to replays???
        //
        // For now: I will just re-use existing movement controllers + physics
        // by relying on the fact that only the player's currently controlled entity gets predicted:
        if (_player.LocalPlayer?.ControlledEntity is { } player && player.IsClientSide() && _entMan.HasComponent<ReplayObserverComponent>(player))
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().Update(args.DeltaSeconds);
            if (_entMan.TryGetComponent(_player.LocalPlayer?.ControlledEntity, out InputMoverComponent? mover))
                mover.LastInputTick = GameTick.Zero;
        }

        if (!Playing || Steps == null)
            return;

        Steps = Steps - 1;
        if (Steps <= 0)
        {
            Playing = false;
            DebugTools.AssertNull(Steps);
        }
    }

    private void ProcessMessages(ReplayMessage replayMessageList, bool skipEffectEvents)
    {
        if (CurrentReplay == null)
            return;

        foreach (var message in replayMessageList.Messages)
        {
            // TODO REPLAYS find a better/more extensible way of handling messages than this giant switch statement.
            switch (message)
            {
                case ReplayPrototypeUploadMsg prototype:
                    CurrentReplay.BlockRewind = true;
                    _netMan.DispatchLocalNetMessage(new GamePrototypeLoadMessage { PrototypeData = prototype.PrototypeData });
                    break;
                case ReplayResourceUploadMsg resource:
                    CurrentReplay.BlockRewind = true;
                    _netMan.DispatchLocalNetMessage(new NetworkResourceUploadMessage { RelativePath = resource.RelativePath, Data = resource.Data });
                    break;
                case ChatMessage chat:
                    // Just pass on the chat message to the UI controller, but skip speech-bubbles if we are fast-forwarding.
                    _uiMan.GetUIController<ChatUIController>().ProcessChatMessage(chat, speechBubble: !skipEffectEvents);
                    break;
                case CvarChangeMsg cvars:
                    _netMan.DispatchLocalNetMessage(new MsgConVars { Tick = _timing.CurTick, NetworkedVars = cvars.ReplicatedCvars });
                    break;
                case RoundEndMessageEvent:

                    if (skipEffectEvents)
                        continue;

                    // TODO REPLAYS handle round end windows properly to prevent window duplication.
                    // The round-end logic just needs to properly track the window. Clients should also be able to
                    // re-open the window after having closed it.

                    if (!_uiMan.WindowRoot.Children.Any(x => x.GetType() == typeof(RoundEndSummaryWindow)))
                        _entMan.DispatchReceivedNetworkMsg((EntityEventArgs) message);
                    break;
                //
                // TODO REPLAYS figure out a cleaner way of doing this. This sucks.
                // Next: we want to avoid spamming animations, sounds, and pop-ups while scrubbing or rewinding time
                // (e.g., to rewind 1 tick, we really rewind ~60 and then fast forward 59). Currently, this is
                // effectively an EntityEvent blacklist. But this is kinda shit and should be done differently somehow.
                // The unifying aspect of these events is that they trigger pop-ups, UI changes, spawn client-side
                // entities or start animations.
                case PopupEvent:
                case AudioMessage:
                case PickupAnimationEvent:
                case MeleeLungeEvent:
                case HitscanEvent:
                case ImpactEffectEvent:
                case MuzzleFlashEvent:
                case DamageEffectEvent:
                    if (!skipEffectEvents)
                        _entMan.DispatchReceivedNetworkMsg((EntityEventArgs)message);
                    break;
                case EntityEventArgs args:
                    // Just raise the event and let systems handle it.
                    _entMan.DispatchReceivedNetworkMsg(args);
                    break;
            }
        }
    }
}
