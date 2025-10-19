// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Magic.Components;

// Added to objects when they are made animate
[RegisterComponent, NetworkedComponent]
public sealed partial class AnimateComponent : Component;
