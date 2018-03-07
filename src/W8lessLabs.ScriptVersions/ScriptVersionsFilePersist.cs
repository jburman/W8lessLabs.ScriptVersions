using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace W8lessLabs.ScriptVersions
{
    public class ScriptVersionsFilePersist
    {
        private string _path;

        public ScriptVersionsFilePersist(string path) =>
            _path = path ?? throw new ArgumentNullException(nameof(path));

        public void Save(ScriptVersionsFile file)
        {
            if (file != null)
                File.WriteAllText(_path, JsonConvert.SerializeObject(file, Formatting.Indented));
        }

        public ScriptVersionsFile Load()
        {
            ScriptVersionsFile returnFile = null;
            if(File.Exists(_path))
            {
                var serializer = new JsonSerializer();
                
                using (var file = File.OpenRead(_path))
                using (var reader = new StreamReader(file))
                using (var json = new JsonTextReader(reader))
                    returnFile = serializer.Deserialize(json, typeof(ScriptVersionsFile)) as ScriptVersionsFile;
            }
            return returnFile;
        }

        public async Task<ScriptVersionsFile> LoadAsync(CancellationToken cancellation = default(CancellationToken))
        {
            ScriptVersionsFile returnFile = null;
            if (File.Exists(_path))
            {
                using (var file = File.OpenRead(_path))
                using (var reader = new StreamReader(file))
                using (var json = new JsonTextReader(reader)) {
                    JObject jsonObj = (await JObject.LoadAsync(json, cancellation).ConfigureAwait(false));
                    returnFile = jsonObj.ToObject<ScriptVersionsFile>();
                }
            }
            return returnFile;
        }

        /// <summary>
        /// Checks if a given date time is newer than what is stored in the json file stored on disk.
        /// </summary>
        /// <param name="newTimeStamp"></param>
        /// <returns></returns>
        public async Task<bool> IsNewer(DateTimeOffset newTimeStamp)
        {
            bool returnNewer = true;

            if(File.Exists(_path))
            {
                string lastUpdatedProperty = nameof(ScriptVersionsFile.LastUpdated);

                using (var file = File.OpenRead(_path))
                using (var reader = new StreamReader(file))
                using (var json = new JsonTextReader(reader))
                {
                    while(await json.ReadAsync().ConfigureAwait(false))
                    {
                        if(json.TokenType == JsonToken.PropertyName && json.Value.ToString() == lastUpdatedProperty)
                        {
                            DateTimeOffset? lastUpdated = await json.ReadAsDateTimeOffsetAsync().ConfigureAwait(false);
                            if (lastUpdated.HasValue && lastUpdated.Value >= newTimeStamp)
                                returnNewer = false;
                            break;
                        }
                    }
                }
            }

            return returnNewer;
        }
    }
}
