// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.IO;
using System.Xml.Serialization;
using Content.Shared.SS220.Photocopier.Forms;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using YamlDotNet.Serialization;

namespace Content.Server.SS220.Photocopier.Forms;

/// <summary>
/// Deserializes photocopier forms on initialization and caches them for future use.
/// Used by server to determine what content to print on based client's request.
/// </summary>
public sealed class FormManager : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    private readonly ISawmill _sawmill = Logger.GetSawmill("form-manager");

        /// <summary>
    /// Path at which index file is located. The file contains names of groups and paths of form XML files.
    /// </summary>
    private readonly ResPath _indexPath = new("/PhotocopierForms/FormIndex.yml");

    private Dictionary<string, Dictionary<string, FormGroup>> _collections = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestPhotocopierFormsMessage>(OnFormTreeRequest);

        DeserializeAllForms();
    }

    /// <summary>
    /// Tries to get the form the way told by the descriptor.
    /// </summary>
    /// <param name="descriptor">Describes where to look for the form</param>
    public Form? TryGetFormFromDescriptor(FormDescriptor descriptor)
    {
        try
        {
            if (!_collections.TryGetValue(descriptor.CollectionId, out var collection))
            {
                _sawmill.Error("Unsuccessful attempt to access collection " + descriptor.CollectionId);
                return null;
            }

            if (!collection.TryGetValue(descriptor.GroupId, out var group))
            {
                _sawmill.Error("Unsuccessful attempt to access group " + descriptor.GroupId + " in collection " + descriptor.CollectionId);
                return null;
            }

            if (!group.Forms.TryGetValue(descriptor.FormId, out var form))
            {
                _sawmill.Error("Unsuccessful attempt to access form " + descriptor.FormId +
                               " in group " + descriptor.GroupId + ", collection " + descriptor.CollectionId);
                return null;
            }

            return form;
        }
        catch (Exception e)
        {
            Logger.ErrorS("form-manager", e, "Failed to access form by descriptor");
        }

        return null;
    }

    /// <summary>
    /// Deserializes an index YAML file to locate the XML files of forms. Deserializes them as well.
    /// Stores results in Collections field which is then ready to be served to clients.
    /// Should be called on init.
    /// </summary>
    private void DeserializeAllForms()
    {
        // Read index
        TryReadAllText(_indexPath, out var indexContentsString);

        if (string.IsNullOrEmpty(indexContentsString))
        {
            _sawmill.Warning("No forms were loaded because no index content was read");
            return;
        }

        // Parse index
        Dictionary<string, Dictionary<string, DeserializedFormGroup>> parsedIndex;
        try
        {
            var yamlDeserializer = new Deserializer();
            parsedIndex = yamlDeserializer
                .Deserialize<Dictionary<string, Dictionary<string, DeserializedFormGroup>>>(indexContentsString);
        }
        catch (Exception e)
        {
            Logger.ErrorS("form-manager", e, "Failed to parse form index");
            return;
        }

        // Reconstruct index using NetSerializable FormGroups & parse forms
        _sawmill.Debug("Starting to reconstruct index and deserialize forms");

        var newCollectionsDict = new Dictionary<string, Dictionary<string, FormGroup>>();
        var xmlDeserializer = new XmlSerializer(typeof(Form));
        var totalAddedForms = 0;
        foreach (var deserializedCollection in parsedIndex)
        {
            var collection = new Dictionary<string, FormGroup>();
            newCollectionsDict.Add(deserializedCollection.Key, collection);

            foreach (var deserializedFormGroup in deserializedCollection.Value)
            {
                var formGroup = new FormGroup(
                    deserializedFormGroup.Value.Name,
                    deserializedFormGroup.Key);

                collection.Add(deserializedFormGroup.Key, formGroup);

                // deserialize forms in a group
                foreach (var formPath in deserializedFormGroup.Value.Forms)
                {
                    TryReadAllText(new ResPath(formPath), out var formXmlContents);
                    if (string.IsNullOrEmpty(formXmlContents))
                    {
                        _sawmill.Error("Form file doesn't exist or is empty at path: " + formPath);
                        continue;
                    }

                    var form = (Form?) xmlDeserializer.Deserialize(new StringReader(formXmlContents));
                    if (form == null)
                    {
                        _sawmill.Error("Failed to deserialize form at path: " + formPath);
                        continue;
                    }

                    if (string.IsNullOrEmpty(form.FormId))
                    {
                        _sawmill.Error("Form ID is null or empty. Form path: " + formPath);
                        continue;
                    }

                    if (formGroup.Forms.ContainsKey(form.FormId))
                    {
                        _sawmill.Error("Duplicated form ID \"" + form.FormId + "\", form wasn't added.");
                        continue;
                    }

                    formGroup.Forms.Add(form.FormId, form);
                    totalAddedForms++;
                }
            }
        }

        _sawmill.Info("Successfully parsed " + totalAddedForms.ToString() + " forms");
        _collections = newCollectionsDict;
    }

    /// <summary>
    /// Reads all text of content file at a given ResPath. Handles exceptions and logs them.
    /// </summary>
    /// <param name="path">Path to content file to read from</param>
    /// <param name="output">String in which read content will be stored.
    /// Can be null if read attempt wasn't successful.</param>
    private void TryReadAllText(ResPath path, out string? output)
    {
        try
        {
            output = _resourceManager.ContentFileReadAllText(path);
        }
        catch (Exception e)
        {
            Logger.ErrorS("form-manager", e, "Failed to read file at " + path);
            output = null;
        }
    }

    private void OnFormTreeRequest(RequestPhotocopierFormsMessage message, EntitySessionEventArgs args)
    {
        _sawmill.Debug("Received photocopier forms request from client");
        var response = new PhotocopierFormsMessage(_collections);
        RaiseNetworkEvent(response, args.SenderSession.ConnectedClient);
    }
}

/// <summary>
/// Exists entirely for deserialization purposes,
/// for the purpose of storing parsed forms a proper FormGroup should be created.
/// </summary>
internal struct DeserializedFormGroup
{
    public string Name;
    public HashSet<string> Forms;
}
