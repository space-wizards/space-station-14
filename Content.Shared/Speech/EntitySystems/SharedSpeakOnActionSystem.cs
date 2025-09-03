using Content.Shared.Chasm;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.Muting;
using System;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Once the chat refactor has happened, move the code from
/// <see cref="Content.Server.Speech.EntitySystems.SpeakOnUseSystem"/>
/// to here and set this class to sealed.
/// </summary>
public abstract class SharedSpeakOnActionSystem : EntitySystem;
