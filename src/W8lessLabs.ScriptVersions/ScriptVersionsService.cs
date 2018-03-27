using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace W8lessLabs.ScriptVersions
{
    public class ScriptVersionsService
    {
        private readonly ScriptVersionsFilePersist _scriptVersionsPersist;
        private ScriptVersionsFile _scriptVersions;

        private ScriptVersionsServiceOptions _options;

        private Dictionary<string, string> _scriptFiles;
        private readonly ConcurrentDictionary<(int, int), string> _versionedScriptsCache;

        private DateTimeOffset _lastLoaded;
        private DateTimeOffset _expirationTime;

        private int _scriptUpdateFlag;

        public ScriptVersionsService(string scriptVersionsFile, ScriptVersionsServiceOptions options = default(ScriptVersionsServiceOptions))
        {
            if (string.IsNullOrEmpty(scriptVersionsFile))
                throw new ArgumentNullException(nameof(scriptVersionsFile));

            if (options == null)
                options = new ScriptVersionsServiceOptions();

            _options = options;

            // the last time that the scripts were reloaded
            _lastLoaded = default(DateTimeOffset);

            // store all of the file paths and their versions
            _scriptFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _scriptVersionsPersist = new ScriptVersionsFilePersist(scriptVersionsFile);

            // holds script paths to calculated full urls with versions (this is cleared whenever versions are reloaded)
            _versionedScriptsCache = new ConcurrentDictionary<(int, int), string>();

            _scriptUpdateFlag = 0;

            _LoadScriptVersions();
        }

        private bool _IsTimeToUpdate() => DateTimeOffset.Now > _expirationTime;

        private void _LoadScriptVersions()
        {
            if (0 == Interlocked.Exchange(ref _scriptUpdateFlag, 1))
            {
                if (_IsTimeToUpdate())
                {
                    var loadVersions = Task.Run(async delegate
                    {
                        if (_IsTimeToUpdate() && (await _scriptVersionsPersist.IsNewerThan(_lastLoaded)))
                        {
                            _scriptVersions = await _scriptVersionsPersist.LoadAsync();
                            var scriptFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                            if (_scriptVersions != null)
                            {
                                void SetFiles(IDictionary<string, string> collection, string name)
                                {
                                    if (_scriptVersions.Files.TryGetValue(name, out FileVersion[] files))
                                    {
                                        foreach (var file in files)
                                            collection[file.Path + "/" + file.Name] = file.Version.ToString();
                                    }
                                }

                                string[] scriptFileTypes = _options.ScriptFileTypes;

                                // if specific types were not specified, then load them all
                                if (scriptFileTypes == null || scriptFileTypes.Length == 0)
                                    scriptFileTypes = _scriptVersions.Files.Keys.ToArray();

                                for (int i = 0; i < scriptFileTypes.Length; i++)
                                    SetFiles(scriptFiles, scriptFileTypes[i]);


                                _scriptFiles = scriptFiles;     // assign new set of script versions
                                _versionedScriptsCache.Clear(); // reset cached URLs

                                _lastLoaded = DateTimeOffset.Now;
                                _expirationTime = _lastLoaded + _options.CacheExpires;
                            }
                        }
                    });

                    Task.WaitAll(loadVersions);
                }
                Interlocked.Exchange(ref _scriptUpdateFlag, 0);
            }
        }

        private bool _IsAbsoluteUrl(string scriptPath) =>
            Uri.IsWellFormedUriString(scriptPath, UriKind.Absolute);

        private string _NormalizeScriptPath(string scriptPath, bool useMinifed)
        {
            if (scriptPath.StartsWith("~/"))
                scriptPath = scriptPath.Substring(2);
            if (scriptPath[0] == '/')
                scriptPath = scriptPath.Substring(1);

            if (useMinifed)
            {
                string ext = Path.GetExtension(scriptPath);
                if (!scriptPath.EndsWith(".min" + ext, StringComparison.OrdinalIgnoreCase))
                    scriptPath = scriptPath.Substring(0, scriptPath.Length - ext.Length) + ".min" + ext;
            }

            return scriptPath;
        }

        private (string version, string normalizedScriptPath) _GetVersion(string scriptPath)
        {
            string version = null;
            string normalizedScriptPath = scriptPath;

            if (_scriptFiles == null)
                return (version, scriptPath);

            if (!string.IsNullOrEmpty(scriptPath))
            {
                normalizedScriptPath = _NormalizeScriptPath(scriptPath, _options.UseMinified);

                if (_scriptFiles != null)
                    _scriptFiles.TryGetValue(normalizedScriptPath, out version);
                
                // if version not found and UseMinified turned on, then fallback and look for the original script name
                if (version == null && _options.UseMinified)
                {
                    normalizedScriptPath = _NormalizeScriptPath(scriptPath, false);
                    _scriptFiles.TryGetValue(normalizedScriptPath, out version);
                }

                if (_IsTimeToUpdate())
                    _LoadScriptVersions();
            }
            return (version, normalizedScriptPath);
        }

        public string GetVersion(string scriptPath)
        {
            (string version, string normalizedScriptPath) = _GetVersion(scriptPath);
            return version;
        }

        public string GetVersionedUrl(string scriptPath, string relativePath = "")
        {
            if (_IsAbsoluteUrl(scriptPath) || scriptPath == null || scriptPath.Length == 0)
                return scriptPath;
            else
            {
                (int, int) scriptCacheKey = (scriptPath.GetHashCode(), relativePath == null ? 0 : relativePath.GetHashCode());
                string versionedUrl = string.Empty;
                if (_versionedScriptsCache.TryGetValue(scriptCacheKey, out versionedUrl))
                    return versionedUrl;
                else
                {
                    (string version, string normalizedPath) = _GetVersion(scriptPath);

                    if (normalizedPath.Length > 1)
                    {
                        if (!string.IsNullOrEmpty(version))
                        {
                            // pre-pend relative path if applicable
                            if (scriptPath[0] != '/' && !string.IsNullOrEmpty(relativePath))
                                normalizedPath = relativePath + (relativePath.EndsWith("/") ? "" : "/") + normalizedPath;
                            else if (scriptPath[0] == '/')
                                normalizedPath = "/" + normalizedPath;

                            if (_options.BaseUrl?.Length > 0)
                            {
                                if (_options.BaseUrl[_options.BaseUrl.Length - 1] == '/') // baseUrl ends with '/'
                                {
                                    if (normalizedPath[0] == '/')
                                        normalizedPath = _options.BaseUrl + normalizedPath.Substring(1); // skip first char which is a '/'
                                    else
                                        normalizedPath = $"{_options.BaseUrl}{normalizedPath}";
                                }
                                else // baseUrl does not end with '/'
                                {
                                    if (normalizedPath[0] == '/')
                                        normalizedPath = _options.BaseUrl + normalizedPath;
                                    else
                                        normalizedPath = $"{_options.BaseUrl}/{normalizedPath}";
                                }
                            }
                        }
                        normalizedPath = $"{normalizedPath}?{_options.VersionParamName}={version}";
                    }

                    // cache it
                    _versionedScriptsCache.TryAdd(scriptCacheKey, normalizedPath);

                    return normalizedPath;
                }
            }
        }
    }
}
