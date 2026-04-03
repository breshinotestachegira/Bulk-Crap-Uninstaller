/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Klocman.Extensions;
using Klocman.Tools;
using Microsoft.Win32;
using UninstallTools.Junk.Confidence;
using UninstallTools.Junk.Containers;

namespace UninstallTools.Junk.Finders.Registry
{
    public class ContextMenuScanner : JunkCreatorBase
    {
        private static readonly string[] ClassesRoots =
        {
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Classes",
            @"HKEY_CURRENT_USER\SOFTWARE\Classes"
        };

        private static readonly string[] ShellSubpaths =
        {
            "shell",
            @"Background\shell"
        };

        private List<ContextMenuEntry> _entries;

        public override void Setup(ICollection<ApplicationUninstallerEntry> allUninstallers)
        {
            base.Setup(allUninstallers);

            _entries = new List<ContextMenuEntry>();

            foreach (var classesRootPath in ClassesRoots)
            {
                using var classesRoot = RegistryTools.OpenRegistryKey(classesRootPath, false, true);
                if (classesRoot == null)
                    continue;

                foreach (var topLevelKeyName in classesRoot.GetSubKeyNames())
                {
                    try
                    {
                        using var topLevelKey = classesRoot.OpenSubKey(topLevelKeyName, false);
                        if (topLevelKey == null)
                            continue;

                        foreach (var shellSubpath in ShellSubpaths)
                        {
                            using var shellKey = topLevelKey.OpenSubKey(shellSubpath, false);
                            if (shellKey == null)
                                continue;

                            CacheShellEntries(shellKey, topLevelKeyName);
                        }
                    }
                    catch (SystemException ex)
                    {
                        Trace.WriteLine($"Failed to inspect context menu entries under {classesRootPath}\\{topLevelKeyName}: {ex}");
                    }
                }
            }
        }

        public override IEnumerable<IJunkResult> FindJunk(ApplicationUninstallerEntry target)
        {
            if (string.IsNullOrWhiteSpace(target.InstallLocation))
                yield break;

            if (UninstallToolsGlobalConfig.IsSystemDirectory(target.InstallLocation))
                yield break;

            foreach (var entry in _entries.Where(x => PathTools.SubPathIsInsideBasePath(target.InstallLocation, x.CommandFilePath, true, true)))
            {
                var junk = new RegistryKeyJunk(entry.VerbKeyPath, target, this);
                junk.Confidence.Add(ConfidenceRecords.ExplicitConnection);

                var confidence = ConfidenceGenerators.GenerateConfidence(entry.MatchName, target)
                    .Concat(ConfidenceGenerators.GenerateConfidence(entry.TopLevelKeyName, target))
                    .Distinct();
                junk.Confidence.AddRange(confidence);

                yield return junk;
            }
        }

        public override string CategoryName => "Context menu entries";

        private void CacheShellEntries(RegistryKey shellKey, string topLevelKeyName)
        {
            foreach (var verbName in shellKey.GetSubKeyNames())
            {
                try
                {
                    using var verbKey = shellKey.OpenSubKey(verbName, false);
                    using var commandKey = verbKey?.OpenSubKey("command", false);
                    var command = commandKey?.GetStringSafe(null);
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    if (!ProcessStartCommand.TryParse(Environment.ExpandEnvironmentVariables(command), out var parsedCommand))
                        continue;

                    _entries.Add(new ContextMenuEntry(
                        verbKey.Name,
                        topLevelKeyName,
                        verbKey.GetStringSafe(null) ?? verbName,
                        parsedCommand.FileName));
                }
                catch (SystemException ex)
                {
                    Trace.WriteLine($"Failed to inspect context menu verb under {shellKey.Name}\\{verbName}: {ex}");
                }
            }
        }

        private sealed record ContextMenuEntry(string VerbKeyPath, string TopLevelKeyName, string MatchName, string CommandFilePath);
    }
}
