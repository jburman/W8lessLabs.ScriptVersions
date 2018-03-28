using System;
using System.Collections.Generic;

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
                            if (oldVersion.Hash != latestVersion.Hash)
                            {
                                updateVersions[j] = oldVersion.IncrementVersion(latestVersion.Hash);
                                scriptVersionsUpdated = true;
                            }
                            else
                                updateVersions[j] = oldVersion;
                        }
                    }
                    if (!foundOldVersion)
                    {
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
