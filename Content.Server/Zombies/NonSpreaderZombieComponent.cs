// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Zombies;

/// <summary>
/// Zombified entities with this component cannot infect other entities by attacking.
/// </summary>
[RegisterComponent]
public sealed partial class NonSpreaderZombieComponent: Component
{

}
