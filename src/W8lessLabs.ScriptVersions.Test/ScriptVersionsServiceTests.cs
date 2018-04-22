using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace W8lessLabs.ScriptVersions.Test
{
    public class ScriptVersionsServiceTests
    {
        private static string _scriptVersionsFile;
        private static string _scriptVersionsFileWithCss;

        static ScriptVersionsServiceTests()
        {
            _scriptVersionsFile = Path.GetFullPath(Path.Combine(Path.Combine(".", "TestData"), "scriptversions.json"));
            _scriptVersionsFileWithCss = Path.GetFullPath(Path.Combine(Path.Combine(".", "TestData"), "scriptversionswithcss.json"));
        }

        [Fact]
        public void CreateScriptVersionsServiceTest()
        {
            bool exThrown = false;

            try
            {
                var service = new ScriptVersionsService(null);
            }
            catch (ArgumentNullException)
            {
                exThrown = true;
            }

            Assert.True(exThrown, "Expected ArgumentNullException to be thrown by constructor");
        }

        [Fact]
        public void CreateScriptVersionsServiceWithDefaultOptionsTest()
        {
            var service = new ScriptVersionsService(_scriptVersionsFile);
            Assert.Null(service.GetVersion(null));
            Assert.Null(service.GetVersion(string.Empty));
            Assert.Null(service.GetVersion("scripts/doesntexist.js"));
            Assert.Equal("5", service.GetVersion("scripts/main.js"));
            Assert.Equal("2", service.GetVersion("scripts/vendor.js"));
            Assert.Equal("3", service.GetVersion("scripts/vendor.min.js"));

            Assert.Equal("5", service.GetVersion("/scripts/main.js"));
            Assert.Equal("5", service.GetVersion("~/scripts/main.js"));

            Assert.Null(service.GetVersion("https://somewhere.com/scripts/main.js"));
            Assert.Null(service.GetVersion("//somewhere.com/scripts/main.js"));

            Assert.Equal("scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js"));
            Assert.Equal("scripts/vendor.js?v=2", service.GetVersionedUrl("scripts/vendor.js"));
            Assert.Equal("scripts/vendor.min.js?v=3", service.GetVersionedUrl("scripts/vendor.min.js"));

            Assert.Equal("/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js"));
            Assert.Equal("scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js"));

            Assert.Equal("scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", null));
            Assert.Equal("apppath/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", "apppath"));
            Assert.Equal("apppath/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js", "apppath"));
            Assert.Equal("apppath/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js", "apppath/"));
            Assert.Equal("/apppath/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js", "/apppath/"));
            Assert.Equal("tenant1/apppath/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", "tenant1/apppath"));

            // relative path should actually be ignored in this case since path starts with a "/"
            Assert.Equal("/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js", "apppath"));
            Assert.Equal("/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js", null));
            Assert.Equal("/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js", string.Empty));
        }

        [Fact]
        public void CreateScriptVersionsServiceUseMinificationTest()
        {
            var service = new ScriptVersionsService(_scriptVersionsFile, 
                new ScriptVersionsServiceOptions()
                {
                    UseMinified = true
                });

            // should always return minified file version and minified file URL when UseMinified set to "true"
            Assert.Equal("3", service.GetVersion("scripts/vendor.js"));
            Assert.Equal("3", service.GetVersion("scripts/vendor.min.js"));
            Assert.Equal("scripts/vendor.min.js?v=3", service.GetVersionedUrl("scripts/vendor.js"));
            Assert.Equal("scripts/vendor.min.js?v=3", service.GetVersionedUrl("scripts/vendor.min.js"));

            // but, a script without a minified version should return the normal version
            Assert.Equal("/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js"));
        }

        [Fact]
        public void CreateScriptVersionsServiceCusomParamNameTest()
        {
            var service = new ScriptVersionsService(_scriptVersionsFile,
                new ScriptVersionsServiceOptions()
                {
                    VersionParamName = "_version"
                });

            Assert.Equal("scripts/main.js?_version=5", service.GetVersionedUrl("scripts/main.js"));
        }

        [Fact]
        public void CreateScriptVersionsServiceWithBaseUrlTest()
        {
            var service = new ScriptVersionsService(_scriptVersionsFile,
                new ScriptVersionsServiceOptions()
                {
                    BaseUrl = "//foo.com/"
                });

            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js"));
            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js"));
            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js"));

            Assert.Equal("//foo.com/apppath/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js", "apppath"));
            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js", "apppath")); // should ignore relative path
            Assert.Equal("//foo.com/apppath/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", "/apppath"));
            Assert.Equal("//foo.com/apppath/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", "/apppath/"));

            service = new ScriptVersionsService(_scriptVersionsFile,
                new ScriptVersionsServiceOptions()
                {
                    BaseUrl = "//foo.com" // removed trailing /
                });

            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js"));
            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js"));
            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js"));

            Assert.Equal("//foo.com/apppath/scripts/main.js?v=5", service.GetVersionedUrl("scripts/main.js", "apppath"));
            Assert.Equal("//foo.com/scripts/main.js?v=5", service.GetVersionedUrl("/scripts/main.js", "apppath")); // should ignore relative path
            Assert.Equal("//foo.com/apppath/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", "/apppath"));
            Assert.Equal("//foo.com/apppath/scripts/main.js?v=5", service.GetVersionedUrl("~/scripts/main.js", "/apppath/"));
        }

        [Fact]
        public void CreateScriptVersionsServiceWithCustomOptionsTest()
        {
            var service = new ScriptVersionsService(_scriptVersionsFile,
                new ScriptVersionsServiceOptions()
                {
                    BaseUrl = "//foo.com/",
                    UseMinified = true,
                    VersionParamName = "ver",
                    ScriptFileTypes = new [] { "js" }
                });

            Assert.Equal("//foo.com/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("scripts/vendor.js"));
            Assert.Equal("//foo.com/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("/scripts/vendor.js"));
            Assert.Equal("//foo.com/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("~/scripts/vendor.min.js"));

            Assert.Equal("//foo.com/apppath/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("scripts/vendor.js", "apppath"));
            Assert.Equal("//foo.com/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("/scripts/vendor.js", "apppath")); // should ignore relative path
            Assert.Equal("//foo.com/apppath/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("~/scripts/vendor.js", "/apppath"));
            Assert.Equal("//foo.com/apppath/scripts/vendor.min.js?ver=3", service.GetVersionedUrl("~/scripts/vendor.min.js", "/apppath/"));
        }

        [Fact]
        public void CreateScriptVersionsServiceWithFileTypeFilterTest()
        {
            var service = new ScriptVersionsService(_scriptVersionsFileWithCss,
                new ScriptVersionsServiceOptions()
                {
                    ScriptFileTypes = new [] { "foo" } // shouldn't match any...
                });

            Assert.Null(service.GetVersion("scripts/main.js"));
            Assert.Null(service.GetVersion("css/main.css"));

            service = new ScriptVersionsService(_scriptVersionsFileWithCss,
                new ScriptVersionsServiceOptions()
                {
                    ScriptFileTypes = new[] { "css" } // css only
                });

            Assert.Null(service.GetVersion("scripts/main.js"));
            Assert.Equal("1", service.GetVersion("css/main.css"));

            service = new ScriptVersionsService(_scriptVersionsFileWithCss,
                new ScriptVersionsServiceOptions()
                {
                    ScriptFileTypes = new[] { "css", "js" }
                });

            Assert.Equal("1", service.GetVersion("scripts/main.js"));
            Assert.Equal("1", service.GetVersion("css/main.css"));

            service = new ScriptVersionsService(_scriptVersionsFileWithCss); // should load all types by default
            Assert.Equal("1", service.GetVersion("scripts/main.js"));
            Assert.Equal("1", service.GetVersion("css/main.css"));
        }

        [Fact]
        public async Task LoadNewVersionFromFile()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "scriptversionstest.json");
            var persist = new ScriptVersionsFilePersist(tempPath);

            try
            {
                var versions = new ScriptVersionsFile();
                bool updated = versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf123",
                        "scripts",
                        1)
                });
                persist.Save(versions);

                var service = new ScriptVersionsService(tempPath, new ScriptVersionsServiceOptions()
                {
                    CacheExpires = TimeSpan.FromMilliseconds(200)
                });

                Assert.Equal("1", service.GetVersion("scripts/test1.js"));
                await Task.Delay(210);
                Assert.Equal("1", service.GetVersion("scripts/test1.js"));

                service = new ScriptVersionsService(tempPath, new ScriptVersionsServiceOptions()
                {
                    CacheExpires = TimeSpan.FromMilliseconds(200)
                });
                Assert.Equal("1", service.GetVersion("scripts/test1.js"));

                versions.SetVersions("js", new[]
                {
                    new FileVersion("test1.js",
                        "asdf123",
                        "scripts",
                        2) // increment version
                });
                persist.Save(versions);

                Assert.Equal("1", service.GetVersion("scripts/test1.js"));
                await Task.Delay(210); // this should let the cache expire
                Assert.Equal("2", service.GetVersion("scripts/test1.js"));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
    }
}
