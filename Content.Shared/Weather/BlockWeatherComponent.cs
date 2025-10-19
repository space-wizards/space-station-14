// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

/// <summary>
/// This entity will block the weather if it's anchored to the floor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockWeatherComponent : Component
{

}
