﻿// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Paper;
using Content.Shared.SS220.Photocopier;
using Content.Shared.SS220.Photocopier.Forms;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Photocopier;

public sealed partial class PhotocopierSystem
{
    /// <summary>
    /// Finds photocopyable components and saves their data.
    /// </summary>
    /// <returns>Dictionary of component types and corresponding data structures.
    /// Ready to be assigned to DataToCopy field of PhotocopierComponent.</returns>
    public Dictionary<Type, IPhotocopiedComponentData> GetDataToCopyFromEntity(EntityUid uid)
    {
        var output = new Dictionary<Type, IPhotocopiedComponentData>();

        var components = _entityManager.GetComponents(uid);
        foreach (var iComponent in components)
        {
            if(iComponent is not IPhotocopyableComponent copyableComponent)
                continue;

            var componentData = copyableComponent.GetPhotocopiedData();
            output.Add(copyableComponent.GetType(), componentData);
        }

        return output;
    }

    /// <summary>
    /// Tries to get photocopyable metadata from entity, which is then used along with DataToCopy to create a photocopy
    /// of an entity.
    /// </summary>
    /// <param name="entity">Entity, whose metadata should be copied</param>
    /// <param name="metaData">If function returns true, this parameter contains Photocopyable metadata,
    /// ready to be assigned to PhotocopierComponent.MetaDataToCopy.</param>
    public bool TryGetPhotocopyableMetaData(
        EntityUid entity,
        [NotNullWhen(true)] out PhotocopyableMetaData? metaData)
    {
        if (!TryComp<MetaDataComponent>(entity, out var metaDataComp))
        {
            metaData = null;
            return false;
        }

        metaData = new PhotocopyableMetaData()
        {
            EntityName = metaDataComp.EntityName,
            EntityDescription = metaDataComp.EntityDescription,
            PrototypeId = metaDataComp.EntityPrototype?.ID
        };
        return true;
    }

    /// <summary>
    /// Finds photocopyable components and restores their data from DataToCopy dictionary.
    /// </summary>
    /// <param name="uid">EntityUid of an entity to which DataToCopy should be written.</param>
    /// <param name="dataToCopy"></param>
    public void RestoreEntityFromData(EntityUid uid, Dictionary<Type, IPhotocopiedComponentData> dataToCopy)
    {
        var components = _entityManager.GetComponents(uid);
        foreach (var iComponent in components)
        {
            if (iComponent is not Component component)
                continue;

            if(component is not IPhotocopyableComponent copyableComponent)
                continue;

            if(!dataToCopy.TryGetValue(copyableComponent.GetType(), out var componentData))
                continue;

            componentData.RestoreFromData(uid, component);
        }
    }

    /// <summary>
    /// Turns deserialized form into sets of component fields, so they later can be made into an entity
    /// after a printing process.
    /// </summary>
    public void FormToDataToCopy(
        Form form,
        out Dictionary<Type, IPhotocopiedComponentData> dataToCopy,
        out PhotocopyableMetaData metaData)
    {
        var paperComponentData = new PaperPhotocopiedData()
        {
            Content = form.Content,
            StampedBy = form.StampedBy,
            StampState = form.StampState
        };

        dataToCopy = new Dictionary<Type, IPhotocopiedComponentData>();
        dataToCopy.Add(typeof(PaperComponent), paperComponentData);

        metaData = new PhotocopyableMetaData()
        {
            EntityName = form.EntityName,
            PrototypeId = form.PrototypeId
        };
    }

    /// <summary>
    /// Spawns a copy of entity at specified coordinates using DataToCopy and MetaDataToCopy.
    /// </summary>
    public EntityUid? SpawnCopy(
        EntityCoordinates at,
        PhotocopyableMetaData? metaDataToCopy,
        Dictionary<Type, IPhotocopiedComponentData>? dataToCopy)
    {
        string entityToSpawn;
        if (metaDataToCopy is not null && !string.IsNullOrEmpty(metaDataToCopy.PrototypeId))
            entityToSpawn = metaDataToCopy.PrototypeId;
        else
            entityToSpawn = "Paper";

        EntityUid printed;
        try
        {
            printed = EntityManager.SpawnEntity(entityToSpawn, at);
        }
        catch (UnknownPrototypeException e)
        {
            _sawmill.Error("Tried to spawn a copy of a document, but got an unknown prototype ID: \""+entityToSpawn+"\"");
            return null;
        }


        if (metaDataToCopy is not null && TryComp<MetaDataComponent>(printed, out var metaData))
        {
            if (!string.IsNullOrEmpty(metaDataToCopy.EntityName))
                metaData.EntityName = metaDataToCopy.EntityName;

            if (!string.IsNullOrEmpty(metaDataToCopy.EntityDescription))
                metaData.EntityDescription = metaDataToCopy.EntityDescription;
        }

        if (dataToCopy is not null)
            RestoreEntityFromData(printed, dataToCopy);

        return printed;
    }

    /// <summary>
    /// Spawns a copy of paper using data cached in PhotocopierComponent.DataToCopy and PhotocopierComponent.MetaDataToCopy.
    /// </summary>
    private void SpawnCopyFromPhotocopier(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var printout = component.DataToCopy;
        if (printout is null)
        {
            _sawmill.Error("Entity " + uid + " tried to spawn a copy of paper, but DataToCopy was null.");
            return;
        }

        SpawnCopy(Transform(uid).Coordinates, component.MetaDataToCopy, printout);
    }
}
