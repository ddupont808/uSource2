using System;
using System.IO;
using SteamDatabase.ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.IO;

namespace uSource2
{
    class VPKLoader : IFileLoader
    {
        public Package[] packages;

        public VPKLoader(string[] packages)
        {
            this.packages = new Package[packages.Length];
            for(int i = 0; i < packages.Length; i++)
            {
                var package = new Package();
                package.Read(packages[i]);
                // package.VerifyHashes();

                this.packages[i] = package;
            }
        }

        public Resource LoadFile(string file)
        {
            Package package = null;
            PackageEntry entry = null;
            
            foreach(var vpk in packages)
            {
                package = vpk;
                entry = vpk.FindEntry(file);
                if (entry != null) break;
            }

            if (entry == null)
                return null;

            package.ReadEntry(entry, out var output, false);

            var resource = new Resource
            {
                FileName = file,
            };
            resource.Read(new MemoryStream(output));

            return resource;
        }
    }
}
