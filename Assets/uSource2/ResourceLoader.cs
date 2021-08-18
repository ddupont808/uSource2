using System;
using System.IO;
using SteamDatabase.ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.IO;

namespace uSource2
{
    class ResourceLoader : IFileLoader
    {
        public string ResourcesPath;

        public ResourceLoader(string path)
        {
            ResourcesPath = path;
        }

        public Resource LoadFile(string file)
        {
            var path = Path.Combine(ResourcesPath, file);

            if (!File.Exists(path))
                return null;

            var resource = new Resource();
            resource.Read(path);

            return resource;
        }
    }
}
