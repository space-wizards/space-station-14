using Content.Server.Humanoid;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Cloning;

/// <summary>
///     System responsible for making a copy of a humanoid's body.
///     For the cloning machines themselves look at CloningPodSystem, CloningConsoleSystem and MedicalScannerSystem instead.
/// </summary>
public sealed partial class CloningSystem : SharedCloningSystem;
