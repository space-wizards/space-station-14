using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Light;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Light;
using Robust.Shared.Utility;
using Robust.Client.Placement;
using Robust.Shared.Map;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Client.Actions;
using Content.Shared.Maps;

namespace Content.Client.Light.Components;

[RegisterComponent]
public sealed class GraphicTogglesComponent : Component
{
    [DataField("toggleFoVActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleFoVActionId = "ToggleFoV";
    public InstantAction? ToggleFoV;

    [DataField("toggleShadowsActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleShadowsActionId = "ToggleShadows";
    public InstantAction? ToggleShadows;

    [DataField("ToggleLightingActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleLightingActionId = "ToggleLighting";
    public InstantAction? ToggleLighting;

    [ViewVariables]
    [DataField("foVAction")]
    public bool FoVAction = false;
    [ViewVariables]
    [DataField("shadowsAction")]
    public bool ShadowsAction = false;
    [ViewVariables]
    [DataField("lightingAction")]
    public bool LightingAction = false;
}

