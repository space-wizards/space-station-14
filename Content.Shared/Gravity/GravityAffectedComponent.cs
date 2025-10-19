// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

/// <summary>
/// This Component allows a target to be considered "weightless" when Weightless is true. Without this component, the
/// target will never be weightless.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GravityAffectedComponent : Component
{
    /// <summary>
    /// If true, this entity will be considered "weightless"
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Weightless = true;
}
