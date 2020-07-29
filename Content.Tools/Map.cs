using System.IO;
using Content.Tools.Handlers;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Tools
{
    public class Map
    {
        public Map(string path)
        {
            Path = path;

            using var reader = new StreamReader(path);
            var stream = new YamlStream();

            stream.Load(reader);

            Root = stream.Documents[0].RootNode;
            EntitiesHandler = new EntitiesHandler(Root);
        }

        public string Path { get; }

        private YamlNode Root { get; }

        public EntitiesHandler EntitiesHandler { get; }

        public MergeResult Merge(Map other)
        {
            return EntitiesHandler.Merge(other);
        }

        public void Save(string fileName)
        {
            using var writer = new StreamWriter(fileName);
            var document = new YamlDocument(Root);
            var stream = new YamlStream(document);
            var emitter = new Emitter(writer);
            var fixer = new TypeTagPreserver(emitter);

            stream.Save(fixer, false);

            writer.Flush();
        }

        public void Save()
        {
            Save(Path);
        }
    }
}
