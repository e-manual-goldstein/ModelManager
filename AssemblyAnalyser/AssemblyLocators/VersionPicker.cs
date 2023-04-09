using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssemblyAnalyser.AssemblyLocators
{
    public static class VersionPicker
    {
        public static string PickBestVersion(string[] versions, string target)
        {
            var targetVersion = new StringVersion(target);
            var availableVersions = versions.Select(n => new StringVersion(n.ToString()));
            string fallbackVersion = null;

            var matchingMajorVersions = availableVersions.Where(v => v.Major == targetVersion.Major);
            var validSuperMajorVersions = availableVersions.Where(v => v.Major > targetVersion.Major);

            //Pick nearest valid version
            fallbackVersion = validSuperMajorVersions
                .OrderBy(d => d.Major)
                .ThenBy(d => d.Minor)
                .ThenBy(t => t.Patch)
                .Select(v => v.FullSemver)
                .FirstOrDefault();
                        
            if (!matchingMajorVersions.Any())
            {
                return fallbackVersion;
            }
            
            var matchingMinorVersions = matchingMajorVersions.Where(v => v.Minor == targetVersion.Minor);
            var superMinorVersions = matchingMajorVersions.Where(v => v.Minor > targetVersion.Minor);

            fallbackVersion = superMinorVersions
                .OrderBy(d => d.Minor)
                .ThenBy(t => t.Patch)
                .Select(v => v.FullSemver)
                .FirstOrDefault() ?? fallbackVersion;

            if (!matchingMinorVersions.Any())
            {
                return fallbackVersion;                
            }

            var matchingPatchVersions = matchingMinorVersions.Where(v => v.Patch == targetVersion.Patch);
            var superPatchVersions = matchingMinorVersions.Where(v => v.Patch > targetVersion.Patch);
            fallbackVersion = superPatchVersions
                .OrderBy(d => d.Patch)
                .Select(d => d.FullSemver)
                .FirstOrDefault() ?? fallbackVersion;
            
            if (matchingPatchVersions.Any())
            {
                if (matchingPatchVersions.Count() > 1) 
                {
                    throw new Exception("Uncharted territory --> How is one PreRelease or BuildMetadata version chosen over another?");
                }
                return matchingPatchVersions.Single().FullSemver;
            }
            return fallbackVersion;
        }

        class StringVersion
        {
            const string versionPattern = @"^(?'major'0|[1-9]\d*)\.(?'minor'0|[1-9]\d*)(\.(?'patch'0|[1-9]\d*))?(?:-(?'prerelease'(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?'buildmetadata'[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
            public StringVersion(string version) 
            {
                FullSemver = version;
                var match = Regex.Match(version, versionPattern);
                if (!match.Success)
                {

                }
                major = match.Groups["major"].Value;
                minor = match.Groups["minor"].Value;
                patch = match.Groups["patch"].Value;
                preRelease = match.Groups["prerelease"].Value;
                buildMetadata = match.Groups["buildmetadata"].Value;
            }

            public override string ToString()
            {
                return FullSemver;
            }

            public string FullSemver;

            string major;
            public int Major => int.Parse(major);

            string minor;
            public int Minor => int.Parse(minor);

            string patch;
            public int Patch => string.IsNullOrEmpty(patch) ? 0 : int.Parse(patch);
            
            string preRelease;
            public string PreRelease => preRelease;

            string buildMetadata;
            public string BuildMetadata => buildMetadata;
            
        }          
    }
}
