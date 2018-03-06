using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace W8lessLabs.ScriptVersions.Test
{
    public class ScriptVersionsFileTests
    {
        [Fact]
        public void SaveNull()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "notused.json");
            var persist = new ScriptVersionsFilePersist(tempPath);
            persist.Save(null); // should do nothing
        }

        [Fact]
        public void LoadNonExistingFile()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "does_not_exist.json");
            var persist = new ScriptVersionsFilePersist(tempPath);
            Assert.Null(persist.Load());
        }


        [Fact]
        public void SaveAndLoad()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "scriptversionstest.json");
            var persist = new ScriptVersionsFilePersist(tempPath);
            DateTimeOffset lastUpdated = DateTimeOffset.Now;

            try
            {
                var versions = new ScriptVersionsFile();
                Assert.Equal(default(DateTimeOffset), versions.LastUpdated);

                bool updated = versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf123",
                        "scripts",
                        1)
                });

                Assert.NotEqual(default(DateTimeOffset), versions.LastUpdated);

                persist.Save(versions);

                var loadedVersions = persist.Load();

                Assert.Equal(versions.LastUpdated, loadedVersions.LastUpdated);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public async Task IsNewer()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "scriptversionstest.json");
            var persist = new ScriptVersionsFilePersist(tempPath);
            DateTimeOffset lastUpdated = DateTimeOffset.Now;

            try
            {
                var versions = new ScriptVersionsFile();

                Assert.True(await persist.IsNewer(lastUpdated));

                Thread.Sleep(1);

                bool updated = versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf123",
                        "scripts",
                        1)
                });

                persist.Save(versions);

                Assert.False(await persist.IsNewer(lastUpdated));

                Assert.True(await persist.IsNewer(DateTimeOffset.Now.AddSeconds(1)));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public void SetVersions()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "scriptversionstest.json");
            var persist = new ScriptVersionsFilePersist(tempPath);

            try
            {
                var versions = new ScriptVersionsFile();
                Assert.Equal(default(DateTimeOffset), versions.LastUpdated);

                bool updated = versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf123",
                        "scripts",
                        1)
                });

                Assert.True(updated);
                Assert.NotEqual(default(DateTimeOffset), versions.LastUpdated);

                Assert.True(versions.Files.Count == 1);
                Assert.True(versions.Files.ContainsKey("js"));
                Assert.True(1 == versions.Files["js"].Length);
                Assert.Equal("test1.js", versions.Files["js"][0].Name);
                Assert.Equal("asdf123", versions.Files["js"][0].Hash);
                Assert.Equal("scripts", versions.Files["js"][0].Path);
                Assert.Equal(1, versions.Files["js"][0].Version);

                // should not update since Hash has not changed
                updated = versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf123",
                        "scripts",
                        1)
                });

                Assert.False(updated, "Expected updated to be False");

                Thread.Sleep(1); // make some time pass so that LastUpdated will change
                var lastUpdated = versions.LastUpdated;

                updated = versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf1234", // changed
                        "scripts",
                        1)
                });

                Assert.True(updated, "Expected updated to be True");
                Assert.True(versions.LastUpdated > lastUpdated, "Expected LastUpdated to change");
                Assert.True(versions.Files.Count == 1, "Expected one file grouping");
                Assert.True(versions.Files.ContainsKey("js"), "Expected Files to contain js grouping");
                Assert.True(1 == versions.Files["js"].Length);
                Assert.Equal("test1.js", versions.Files["js"][0].Name);
                Assert.Equal("asdf1234", versions.Files["js"][0].Hash);
                Assert.Equal("scripts", versions.Files["js"][0].Path);
                Assert.Equal(2, versions.Files["js"][0].Version); // this should increment

                Thread.Sleep(1);
                lastUpdated = versions.LastUpdated;

                // add another grouping
                updated = versions.SetVersions("css", new[]
                {
                    new FileVersion("styles.css",
                        "asdf1234",
                        "css",
                        1)
                });

                Assert.True(updated, "Expected updated to be True");
                Assert.True(versions.LastUpdated > lastUpdated, "Expected LastUpdated to change");
                Assert.True(versions.Files.Count == 2, "Expected two file groupings");
                Assert.True(versions.Files.ContainsKey("css"), "Expected Files to contain css grouping");
                Assert.True(1 == versions.Files["css"].Length, "Expected one css file");
                Assert.Equal("styles.css", versions.Files["css"][0].Name);
                Assert.Equal("asdf1234", versions.Files["css"][0].Hash);
                Assert.Equal("css", versions.Files["css"][0].Path);
                Assert.Equal(1, versions.Files["css"][0].Version);

                Thread.Sleep(1);
                lastUpdated = versions.LastUpdated;

                // add a second script to grouping
                updated = versions.SetVersions("css", new[]
                {
                    versions.Files["css"][0],
                    new FileVersion("styles2.css",
                        "qwerty5",
                        "css",
                        1)
                });

                Assert.True(updated, "Expected updated to be True");
                Assert.True(versions.LastUpdated > lastUpdated, "Expected LastUpdated to change");
                Assert.True(versions.Files.Count == 2, "Expected two file groupings");
                Assert.True(versions.Files.ContainsKey("css"), "Expected Files to contain css grouping");
                Assert.True(2 == versions.Files["css"].Length, "Expected two css files");
                Assert.Equal("styles.css", versions.Files["css"][0].Name);
                Assert.Equal("asdf1234", versions.Files["css"][0].Hash);
                Assert.Equal("css", versions.Files["css"][0].Path);
                Assert.Equal(1, versions.Files["css"][0].Version);
                Assert.Equal("styles2.css", versions.Files["css"][1].Name);
                Assert.Equal("qwerty5", versions.Files["css"][1].Hash);
                Assert.Equal("css", versions.Files["css"][1].Path);
                Assert.Equal(1, versions.Files["css"][1].Version);

                Thread.Sleep(1);
                lastUpdated = versions.LastUpdated;

                updated = versions.SetVersions("css", new[]
                {
                    new FileVersion("styles.css",
                        "asdf12345", // changed
                        "css",
                        1),
                    new FileVersion("styles2.css",
                        "qwerty5",
                        "css",
                        1)
                });
                Assert.True(updated, "Expected updated to be True");
                Assert.True(versions.LastUpdated > lastUpdated, "Expected LastUpdated to change");
                Assert.True(versions.Files.Count == 2, "Expected two file groupings");
                Assert.True(versions.Files.ContainsKey("css"), "Expected Files to contain css grouping");
                Assert.True(2 == versions.Files["css"].Length, "Expected two css files");
                Assert.Equal("styles.css", versions.Files["css"][0].Name);
                Assert.Equal("asdf12345", versions.Files["css"][0].Hash);
                Assert.Equal("css", versions.Files["css"][0].Path);
                Assert.Equal(2, versions.Files["css"][0].Version);

                // remove a script from the group
                updated = versions.SetVersions("css", new[]
                {
                    new FileVersion("styles2.css",
                        "qwerty5",
                        "css",
                        1)
                });

                Assert.True(updated, "Expected updated to be True");
                Assert.True(versions.LastUpdated > lastUpdated, "Expected LastUpdated to change");
                Assert.True(versions.Files.Count == 2, "Expected two file groupings");
                Assert.True(versions.Files.ContainsKey("css"), "Expected Files to contain css grouping");
                Assert.True(1 == versions.Files["css"].Length, "Expected one css file");
                Assert.Equal("styles2.css", versions.Files["css"][0].Name);
                Assert.Equal("qwerty5", versions.Files["css"][0].Hash);
                Assert.Equal("css", versions.Files["css"][0].Path);
                Assert.Equal(1, versions.Files["css"][0].Version);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
    }
}
