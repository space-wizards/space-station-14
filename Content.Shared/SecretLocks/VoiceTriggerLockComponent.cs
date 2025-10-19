// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.SecretLocks;

/// <summary>
/// "Locks" items (Doesn't actually lock them but just switches various settings) so its not possible to tell
/// the item is triggered by a voice activation.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoiceTriggerLockComponent : Component;
