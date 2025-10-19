// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.RequiresGrid;

/// <summary>
/// Destroys an entity when they no longer are part of a Grid
/// </summary>
[RegisterComponent]
[Access(typeof(RequiresGridSystem))]
public sealed partial class RequiresGridComponent : Component
{

}
