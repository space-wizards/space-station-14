// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a nuke operative.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NukeopsRoleComponent : BaseMindRoleComponent;
