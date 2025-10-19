// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will gib the entity when triggered.
/// If TargetUser is true the user will be gibbed instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GibOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Should gibbing also delete the owners items?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteItems = false;
}
