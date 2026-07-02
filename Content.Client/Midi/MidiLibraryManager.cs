using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Midi;

/// <summary>
/// Handles storage/management of MIDI files stored inside the user data directory.
/// </summary>
[PublicAPI]
public sealed partial class MidiLibraryManager : IPostInjectInit
{
    /// <summary>
    /// Directory path to use inside UserData for storing MIDIs.
    /// </summary>
    private static readonly ResPath UserMidiDirectory = new("/UserMidis/");

    [Dependency] private IResourceManager _resManager = default!;

    private readonly List<string> _fileList = [];

    /// <summary>
    /// Raised after a MIDI file has been added to the library. Contains added file name as argument.
    /// </summary>
    public event Action<string>? MidiFileAdded;

    /// <summary>
    /// Raised after a MIDI file has been removed from the library. Contains removed file name as argument.
    /// </summary>
    public event Action<string>? MidiFileRemoved;

    /// <summary>
    /// Raised after more than one or two library changes occured at once. (i.e. initial load or when all items deleted)
    /// </summary>
    public event Action? MidiFilesReset;

    ///<inheritdoc cref="IPostInjectInit"/>
    public void PostInject()
    {
        EnsureMidiDirectoryExists();
        ReloadLibrary();
    }

    /// <summary>
    /// Returns the binary content of the given MIDI file.
    /// </summary>
    /// <param name="fileName">MIDI file name to get.</param>
    /// <returns>MIDI binary as a byte array or an empty byte array if the file doesn't exist.</returns>
    public byte[] GetMidiData(string fileName)
    {
        try
        {
            var filePath = new ResPath(UserMidiDirectory + fileName);
            return _resManager.UserData.ReadAllBytes(filePath);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Returns an enumeration of all available MIDI files.
    /// </summary>
    /// <returns>IEnumerable of MIDI file name strings.</returns>
    public IEnumerable<string> GetMidiFiles()
    {
        return _fileList;
    }

    /// <summary>
    /// Stores the given byte stream with the given file name inside the <see cref="UserMidiDirectory"/> directory.
    /// </summary>
    /// <param name="fileName">File name to write.</param>
    /// <param name="data">Binary data to write.</param>
    /// <remarks>Raises <see cref="MidiFileAdded"/> on success.</remarks>
    public async Task AddMidiFile(string fileName, Stream data)
    {
        try
        {
            await using var file = _resManager.UserData.OpenWrite(new ResPath(UserMidiDirectory + fileName));
            await data.CopyToAsync(file);
            _fileList.Add(fileName);
            MidiFileAdded?.Invoke(fileName);
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Stores the given byte array with the given file name inside the <see cref="UserMidiDirectory"/> directory.
    /// </summary>
    /// <param name="fileName">File name to write.</param>
    /// <param name="data">Binary data to write.</param>
    /// <remarks>Raises <see cref="MidiFileAdded"/> on success.</remarks>
    public async Task AddMidiFile(string fileName, byte[] data)
    {
        await AddMidiFile(fileName, new MemoryStream(data));
    }

    /// <summary>
    /// Renames a MIDI file inside the library.
    /// </summary>
    /// <param name="oldName">Current file name</param>
    /// <param name="newName">New file name</param>
    /// <remarks>Raises <see cref="MidiFileRemoved"/> and <see cref="MidiFileAdded"/> on success.</remarks>
    public void RenameMidiFile(string oldName, string newName)
    {
        try
        {
            var oldPath = new ResPath(UserMidiDirectory + oldName);
            var newPath = new ResPath(UserMidiDirectory + newName);
            oldPath = oldPath.Clean();
            newPath = newPath.Clean();
            _resManager.UserData.Rename(oldPath, newPath);
            _fileList.Remove(oldName);
            MidiFileRemoved?.Invoke(oldName);
            _fileList.Add(newName);
            MidiFileAdded?.Invoke(newName);
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Permanently removes the given MIDI file if it exists inside <see cref="UserMidiDirectory"/>.
    /// </summary>
    /// <param name="fileName">File name to remove.</param>
    /// <remarks>Raises <see cref="MidiFileRemoved"/></remarks>
    public void RemoveMidiFile(string fileName)
    {
        DeleteMidiFile(fileName);
        _fileList.Remove(fileName);
        MidiFileRemoved?.Invoke(fileName);
    }

    /// <summary>
    /// Removes all registered MIDI files, permanently.
    /// </summary>
    /// <remarks>Raises <see cref="MidiFilesReset"/></remarks>
    public void RemoveAllMidiFiles()
    {
        foreach (var fileName in _fileList)
        {
            DeleteMidiFile(fileName);
        }
        _fileList.Clear();
        MidiFilesReset?.Invoke();
    }

    /// <summary>
    /// Clears and reloads the entire MIDI library.
    /// </summary>
    public void ReloadLibrary()
    {
        _fileList.Clear();
        if (!_resManager.UserData.IsDir(UserMidiDirectory))
            return;

        foreach (var path in _resManager.UserData.DirectoryEntries(UserMidiDirectory))
        {
            var filePath = new ResPath(UserMidiDirectory + path);
            if (!filePath.Extension.Equals("midi") && !filePath.Extension.Equals("mid"))
                continue;

            _fileList.Add(filePath.Filename);
        }

        MidiFilesReset?.Invoke();
    }

    private void DeleteMidiFile(string fileName)
    {
        var path = new ResPath(UserMidiDirectory + fileName).Clean();
        _resManager.UserData.Delete(path);
    }

    private void EnsureMidiDirectoryExists()
    {
        if (!_resManager.UserData.Exists(UserMidiDirectory))
            _resManager.UserData.CreateDir(UserMidiDirectory);
    }
}
