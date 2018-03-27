using System;

namespace W8lessLabs.ScriptVersions
{
    public class ScriptVersionsServiceOptions
    {
        public ScriptVersionsServiceOptions()
        {
            CacheExpires = TimeSpan.FromMinutes(30);
            VersionParamName = "v";
        }

        public string[] ScriptFileTypes;
        public TimeSpan CacheExpires;
        public bool UseMinified;
        public string BaseUrl;
        public string VersionParamName;
    }
}
