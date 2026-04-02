/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.IO;
using Klocman.Extensions;
using Klocman.Tools;

namespace UninstallTools.Factory.InfoAdders
{
    public class InstallDateAdder : IMissingInfoAdder
    {
        public void AddMissingInformation(ApplicationUninstallerEntry target)
        {
            if (TryGetRegistryInstallDate(target, out var installDate) || TryGetFilesystemInstallDate(target, out installDate))
                target.InstallDate = installDate;
            else
                target.InstallDate = DateTime.MinValue;
        }

        public string[] RequiredValueNames { get; } = {
            nameof(ApplicationUninstallerEntry.RegistryPath),
            nameof(ApplicationUninstallerEntry.InstallLocation),
            nameof(ApplicationUninstallerEntry.UninstallerFullFilename)
        };
        public bool RequiresAllValues { get; } = false;
        public bool AlwaysRun { get; } = false;

        public string[] CanProduceValueNames { get; } = {
            nameof(ApplicationUninstallerEntry.InstallDate)
        };

        public InfoAdderPriority Priority { get; } = InfoAdderPriority.RunLast;

        private static bool TryGetFilesystemInstallDate(ApplicationUninstallerEntry target, out DateTime result)
        {
            result = DateTime.MinValue;

            try
            {
                if (Directory.Exists(target.InstallLocation))
                {
                    result = Directory.GetCreationTime(target.InstallLocation);
                    return true;
                }

                if (File.Exists(target.UninstallerFullFilename))
                {
                    result = File.GetCreationTime(target.UninstallerFullFilename);
                    return true;
                }
            }
            catch
            {
                result = DateTime.MinValue;
            }

            return false;
        }

        private static bool TryGetRegistryInstallDate(ApplicationUninstallerEntry target, out DateTime result)
        {
            result = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(target.RegistryPath))
                return false;

            try
            {
                using var key = RegistryTools.OpenRegistryKey(target.RegistryPath);
                return key != null && key.TryGetLastWriteTime(out result);
            }
            catch
            {
                result = DateTime.MinValue;
                return false;
            }
        }
    }
}
