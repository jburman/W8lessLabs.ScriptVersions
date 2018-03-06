namespace W8lessLabs.ScriptVersions
{
    public class FileVersion
    {
        public FileVersion()
        {
            Version = 1;
        }

        public FileVersion(string name, string hash, string path, int version)
        {
            Name = name;
            Hash = hash;
            Path = path;
            Version = version;
        }


        public string Name { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public int Version { get; set; }

        public FileVersion IncrementVersion(string newHash) =>
            new FileVersion()
            {
                Name = Name,
                Path = Path,
                Hash = newHash,
                Version = Version + 1
            };

        public FileVersion CloneWithVersion(int version) =>
            new FileVersion()
            {
                Name = Name,
                Path = Path,
                Hash = Hash,
                Version = version
            };
    }
}
