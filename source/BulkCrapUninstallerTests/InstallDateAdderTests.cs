using System;
using Klocman.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UninstallTools;
using UninstallTools.Factory.InfoAdders;

namespace BulkCrapUninstallerTests
{
    [TestClass]
    public class InstallDateAdderTests
    {
        [TestMethod]
        public void AddMissingInformation_PrefersRegistryLastWriteTimeForRegisteredEntries()
        {
            var keyPath = @"HKCU\Software\BCU\Tests\InstallDateAdder_" + Guid.NewGuid().ToString("N");

            try
            {
                using (var key = RegistryTools.CreateSubKeyRecursively(keyPath))
                {
                    key.SetValue("DisplayName", "InstallDateAdder Test");
                }

                var entry = new ApplicationUninstallerEntry
                {
                    RegistryPath = keyPath
                };

                new InstallDateAdder().AddMissingInformation(entry);

                Assert.AreNotEqual(DateTime.MinValue, entry.InstallDate);
                Assert.IsTrue((DateTime.Now - entry.InstallDate).Duration() < TimeSpan.FromMinutes(5));
            }
            finally
            {
                RegistryTools.RemoveRegistryKey(keyPath);
            }
        }
    }
}
