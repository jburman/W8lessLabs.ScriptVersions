using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace W8lessLabs.ScriptVersions
{
    public class ScriptVersionsFile
    {
        public ScriptVersionsFile()
        {
            Files = new Dictionary<string, FileVersion[]>(StringComparer.OrdinalIgnoreCase);
        }

        public DateTimeOffset LastUpdated { get; set; }
        public Dictionary<string, FileVersion[]> Files { get; set; }

        /// <summary>
        /// Updates a group of FileVersions if they have changed compared to what is currently held in the Files collection.
        /// </summary>
        /// <param name="fileGroup">The name of the file group to update.</param>
        /// <param name="latestVersions">The list of file versions to add or update.</param>
        /// <returns>True if changes were made; otherwise, false.</returns>
        public bool SetVersions(string fileGroup, FileVersion[] latestVersions)
        {
            Debug.WriteLine("SetVersions");

            bool scriptVersionsUpdated = false;

            FileVersion[] oldVersions = null;
            if (Files.TryGetValue(fileGroup, out oldVersions))
            {
                FileVersion[] updateVersions = oldVersions;
                // re-dimension to hold the correct number
                if (updateVersions.Length != latestVersions.Length)
                {
                    scriptVersionsUpdated = true;
                    updateVersions = new FileVersion[latestVersions.Length];
                }

                for (int j = 0; j < latestVersions.Length; j++)
                {
                    FileVersion latestVersion = latestVersions[j];

                    // look for an existing entry for the file
                    bool foundOldVersion = false;
                    for (int k = 0; k < oldVersions.Length; k++)
                    {
                        var oldVersion = oldVersions[k];

                        if (oldVersion.FileNamesMatch(latestVersion))
                        {
                            foundOldVersion = true;
                            // default case is to compare hashes and then update the version
                            if (oldVersion.Hash != latestVersion.Hash)
                            {
                                Debug.WriteLine("Old hash does not match new hash. Incrementing version - {0} - {1} - v{2} != {3} - v{4}", 
                                    oldVersion.Name, 
                                    oldVersion.Hash,
                                    oldVersion.Version,
                                    latestVersion.Hash,
                                    latestVersion.Version);

                                // if the version supplied is not greater than the existing version, then increment the existing one
                                if (oldVersion.Version >= latestVersion.Version)
                                    updateVersions[j] = oldVersion.IncrementVersion(latestVersion.Hash);
                                else // otherwise, use the new version that was supplied
                                    updateVersions[j] = latestVersion.CloneWithVersion(latestVersion.Version);

                                updateVersions[j] = oldVersion.IncrementVersion(latestVersion.Hash);
                                scriptVersionsUpdated = true;
                            }
                            // allow version numbers to be updated if requested
                            else if(oldVersion.Version != latestVersion.Version)
                            {
                                Debug.WriteLine("Version numbers do not match. Updating version - {0} - v{1} != v{2}",
                                    oldVersion.Name,
                                    oldVersion.Version,
                                    latestVersion.Version);

                                updateVersions[j] = latestVersion.CloneWithVersion(latestVersion.Version);
                                scriptVersionsUpdated = true;
                            }
                            else
                            {
                                Debug.WriteLine("Old hash matches new hash - {0} - {1}", oldVersion.Name, oldVersion.Hash);

                                updateVersions[j] = oldVersion;
                            }
                        }
                    }
                    if (!foundOldVersion)
                    {
                        Debug.WriteLine("SetVersions - old version not found so adding new entry - {0} - {1}", latestVersions[j].Name, latestVersions[j].Version);

                        scriptVersionsUpdated = true;
                        updateVersions[j] = latestVersions[j].CloneWithVersion(1);
                    }
                }
                Files[fileGroup] = updateVersions;
            }
            else
            {
                FileVersion[] clone = new FileVersion[latestVersions.Length];
                for (int i = 0; i < latestVersions.Length; i++)
                    clone[i] = latestVersions[i].CloneWithVersion(1);

                Files[fileGroup] = clone;
                scriptVersionsUpdated = true;
            }

            if (scriptVersionsUpdated)
                LastUpdated = DateTimeOffset.Now;

            return scriptVersionsUpdated;
        }
    }
}
