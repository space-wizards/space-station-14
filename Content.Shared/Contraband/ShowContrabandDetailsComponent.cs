// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Contraband;

/// <summary>
/// This component allows you to see Contraband details on examine items
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowContrabandDetailsComponent : Component;
