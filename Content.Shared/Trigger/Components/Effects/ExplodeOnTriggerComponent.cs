// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Explosion.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will explode using the entity's <see cref="ExplosiveComponent"/> when triggered.
/// TargetUser will only work of the user has ExplosiveComponent as well.
/// The User will be logged in the admin logs.
/// </summary>
/// <seealso cref="ExplosionOnTriggerComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplodeOnTriggerComponent : BaseXOnTriggerComponent;
