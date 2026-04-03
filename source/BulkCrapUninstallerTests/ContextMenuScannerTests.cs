using System;
using System.IO;
using System.Linq;
using Klocman.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UninstallTools;
using UninstallTools.Junk.Finders.Registry;

namespace BulkCrapUninstallerTests
{
    [TestClass]
    public class ContextMenuScannerTests
    {
        [TestMethod]
        public void FindJunk_FindsCommandVerbUnderShell()
        {
            var installDir = CreateTempInstallDirectory();
            var verbKeyPath = @"HKCU\SOFTWARE\Classes\*\shell\BCUTestOpenWith";

            try
            {
                CreateContextMenuVerb(verbKeyPath, Path.Combine(installDir, "HxD.exe"));

                var target = new ApplicationUninstallerEntry
                {
                    RawDisplayName = "HxD",
                    InstallLocation = installDir
                };

                var scanner = new ContextMenuScanner();
                scanner.Setup(new[] { target });

                var results = scanner.FindJunk(target).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(verbKeyPath.Replace("HKCU", "HKEY_CURRENT_USER"), results[0].GetDisplayName());
            }
            finally
            {
                CleanupContextMenuVerb(verbKeyPath);
                CleanupDirectory(installDir);
            }
        }

        [TestMethod]
        public void FindJunk_FindsCommandVerbUnderBackgroundShell()
        {
            var installDir = CreateTempInstallDirectory();
            var verbKeyPath = @"HKCU\SOFTWARE\Classes\Directory\Background\shell\BCUTestBackground";

            try
            {
                CreateContextMenuVerb(verbKeyPath, Path.Combine(installDir, "HxD.exe"));

                var target = new ApplicationUninstallerEntry
                {
                    RawDisplayName = "HxD",
                    InstallLocation = installDir
                };

                var scanner = new ContextMenuScanner();
                scanner.Setup(new[] { target });

                var results = scanner.FindJunk(target).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(verbKeyPath.Replace("HKCU", "HKEY_CURRENT_USER"), results[0].GetDisplayName());
            }
            finally
            {
                CleanupContextMenuVerb(verbKeyPath);
                CleanupDirectory(installDir);
            }
        }

        private static string CreateTempInstallDirectory()
        {
            var installDir = Path.Combine(Path.GetTempPath(), "BCU_ContextMenuScanner_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(installDir);
            return installDir;
        }

        private static void CreateContextMenuVerb(string verbKeyPath, string executablePath)
        {
            using (var verbKey = RegistryTools.CreateSubKeyRecursively(verbKeyPath))
            {
                verbKey.SetValue(null, "Open with HxD");
            }

            using (var commandKey = RegistryTools.CreateSubKeyRecursively(verbKeyPath + @"\command"))
            {
                commandKey.SetValue(null, $"\"{executablePath}\" \"%1\"");
            }
        }

        private static void CleanupContextMenuVerb(string verbKeyPath)
        {
            RegistryTools.RemoveRegistryKey(verbKeyPath);
        }

        private static void CleanupDirectory(string installDir)
        {
            if (Directory.Exists(installDir))
                Directory.Delete(installDir, true);
        }
    }
}
