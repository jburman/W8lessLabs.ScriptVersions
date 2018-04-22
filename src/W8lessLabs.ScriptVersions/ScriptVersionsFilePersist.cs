using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
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

        public static ScriptVersionsFile LoadFromStream(Stream stream)
        {
            var serializer = new JsonSerializer();
            using (var reader = new StreamReader(stream))
            using (var json = new JsonTextReader(reader))
                return serializer.Deserialize(json, typeof(ScriptVersionsFile)) as ScriptVersionsFile;
        }

        public ScriptVersionsFile Load()
        {
            ScriptVersionsFile returnFile = null;
            if(File.Exists(_path))
                using (var stream = File.OpenRead(_path))
                    returnFile = LoadFromStream(stream);
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
        /// Checks if a the json file stored on disk is newer than the passed in timestamp.
        /// </summary>
        /// <param name="compareTimeStamp"></param>
        /// <returns></returns>
        public async Task<bool> IsNewerThan(DateTimeOffset compareTimeStamp)
        {
            Debug.WriteLine("IsNewerThan - {0}", compareTimeStamp);

            bool returnNewer = false;

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

                            Debug.WriteLine("Last update time from file - {0}", lastUpdated);

                            if (lastUpdated.HasValue && lastUpdated.Value >= compareTimeStamp)
                            {
                                returnNewer = true;

                                Debug.WriteLine("File time is newer");
                            }
                            else
                            {
                                Debug.WriteLine("File time is older");
                            }

                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine("File did not exist - {0}", _path);
            }

            return returnNewer;
        }
    }
}
