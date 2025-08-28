using Content.Shared.Holopad;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private ProtoId<StationAiCustomizationGroupPrototype> _stationAiCoreCustomGroupProtoId = "StationAiCoreIconography";
    private ProtoId<StationAiCustomizationGroupPrototype> _stationAiHologramCustomGroupProtoId = "StationAiHolograms";

    private void InitializeCustomization()
    {
        SubscribeLocalEvent<StationAiCoreComponent, StationAiCustomizationMessage>(OnStationAiCustomization);
    }

    private void OnStationAiCustomization(Entity<StationAiCoreComponent> entity, ref StationAiCustomizationMessage args)
    {
        if (!_protoManager.TryIndex(args.GroupProtoId, out var groupPrototype) || !_protoManager.TryIndex(args.CustomizationProtoId, out var customizationProto))
            return;

        if (!TryGetHeld((entity, entity.Comp), out var held))
            return;

        if (!TryComp<StationAiCustomizationComponent>(held, out var stationAiCustomization))
            return;

        if (stationAiCustomization.ProtoIds.TryGetValue(args.GroupProtoId, out var protoId) && protoId == args.CustomizationProtoId)
            return;

        stationAiCustomization.ProtoIds[args.GroupProtoId] = args.CustomizationProtoId;

        Dirty(held, stationAiCustomization);

        // Update hologram
        if (groupPrototype.Category == StationAiCustomizationType.Hologram)
            UpdateHolographicAvatar((held, stationAiCustomization));

        // Update core iconography
        if (groupPrototype.Category == StationAiCustomizationType.CoreIconography && TryComp<StationAiHolderComponent>(entity, out var stationAiHolder))
            UpdateAppearance((entity, stationAiHolder));
    }

    private void UpdateHolographicAvatar(Entity<StationAiCustomizationComponent> entity)
    {
        if (!TryComp<HolographicAvatarComponent>(entity, out var avatar))
            return;

        if (!entity.Comp.ProtoIds.TryGetValue(_stationAiHologramCustomGroupProtoId, out var protoId))
            return;

        if (!_protoManager.TryIndex(protoId, out var prototype))
            return;

        if (!prototype.LayerData.TryGetValue(StationAiState.Hologram.ToString(), out var layerData))
            return;

        avatar.LayerData = [layerData];
        Dirty(entity, avatar);
    }

    private void CustomizeAppearance(Entity<StationAiCoreComponent> entity, StationAiState state)
    {
        var stationAi = GetInsertedAI(entity);

        if (stationAi == null)
        {
            _appearance.RemoveData(entity.Owner, StationAiVisualLayers.Icon);
            return;
        }

        if (!TryComp<StationAiCustomizationComponent>(stationAi, out var stationAiCustomization) ||
            !TryGetCustomizedAppearanceData((stationAi.Value, stationAiCustomization), out var layerData) ||
            !layerData.TryGetValue(state.ToString(), out var stateData))
        {
            return;
        }

        // This data is handled manually in the client StationAiSystem
        _appearance.SetData(entity.Owner, StationAiVisualLayers.Icon, stateData);
    }

    /// <summary>
    /// Returns a dictionary containing the station AI's appearance for different states.
    /// </summary>
    /// <param name="entity">The station AI.</param>
    /// <param name="layerData">The apperance data, indexed by possible AI states.</param>
    /// <returns>True if the apperance data was found.</returns>
    public bool TryGetCustomizedAppearanceData(Entity<StationAiCustomizationComponent> entity, [NotNullWhen(true)] out Dictionary<string, PrototypeLayerData>? layerData)
    {
        layerData = null;

        if (!entity.Comp.ProtoIds.TryGetValue(_stationAiCoreCustomGroupProtoId, out var protoId) ||
            !_protoManager.TryIndex(protoId, out var prototype) ||
            prototype.LayerData.Count == 0)
        {
            return false;
        }

        layerData = prototype.LayerData;

        return true;
    }
}
