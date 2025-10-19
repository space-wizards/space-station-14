// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// This trigger removes all the fire stacks on a target with <see cref="FlammableComponent"/>.
/// If TargetUser is true, the entity that caused this trigger will be extinguished instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExtinguishOnTriggerComponent : BaseXOnTriggerComponent;
