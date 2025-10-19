// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.NPC;

/// <summary>
/// Added to NPCs that are actively being updated.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveNPCComponent : Component {}
