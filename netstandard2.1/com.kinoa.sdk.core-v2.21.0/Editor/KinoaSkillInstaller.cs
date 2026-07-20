using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Kinoa.Editor
{
    /// <summary>
    ///     Mirrors .claude/skills/kinoa/ from the com.kinoa.sdk.core package into the project root
    ///     so Claude Code can discover the /kinoa skill.
    ///     <para>Hard-overwrites on package version change. Local edits are lost by design.</para>
    ///     <para>
    ///     The installed-version marker is a PER-PROJECT stamp file in Library/ (machine-local,
    ///     never committed). A global EditorPrefs key is deliberately NOT used: it is shared
    ///     across every Unity project on the machine, so switching between projects that embed
    ///     different SDK versions would re-fire the installer on each switch and wipe the skill
    ///     back to that project's bundled copy. The legacy global key is cleaned up on sight.
    ///     </para>
    /// </summary>
    [InitializeOnLoad]
    internal static class KinoaSkillInstaller
    {
        private const string PackageName = "com.kinoa.sdk.core";
        private const string SkillPath = ".claude/skills/kinoa";
        private const string StampFileName = "KinoaSkillInstaller.stamp";

        /// <summary>Pre-stamp machine-global marker — migrated away from; removed when seen.</summary>
        private const string LegacyVersionKey = "Kinoa.SdkCore.InstalledSkillVersion";

        static KinoaSkillInstaller() => EditorApplication.delayCall += Run;

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string StampPath => Path.Combine(ProjectRoot, "Library", StampFileName);

        [MenuItem("Tools/Kinoa/Reinstall AI Integration Skill")]
        private static void ForceReinstall()
        {
            if (File.Exists(StampPath)) File.Delete(StampPath);
            EditorPrefs.DeleteKey(LegacyVersionKey);
            Run();
        }

        private static void Run()
        {
            var pkg = PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == PackageName);
            if (pkg == null)
            {
                Debug.LogWarning($"[Kinoa] Package {PackageName} not found.");
                return;
            }

            // One-time migration off the machine-global marker.
            if (EditorPrefs.HasKey(LegacyVersionKey)) EditorPrefs.DeleteKey(LegacyVersionKey);

            var target = Path.Combine(ProjectRoot, SkillPath);

            // Up to date only when BOTH hold: the per-project stamp matches this package's
            // version AND the skill folder actually exists (a manually deleted folder must
            // reinstall even with a valid stamp).
            var installedVersion = File.Exists(StampPath) ? File.ReadAllText(StampPath).Trim() : null;
            if (installedVersion == pkg.version && Directory.Exists(target))
            {
                return;
            }

            var source = Path.Combine(pkg.resolvedPath, SkillPath);
            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Kinoa] Skill source not found at \"{source}\".");
                return;
            }

            try
            {
                if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
                CopyDir(source, target);
                File.WriteAllText(StampPath, pkg.version);
                Debug.Log($"[Kinoa] AI skill installed to \"{target}\" (v{pkg.version}). Type /kinoa in Claude Code.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Kinoa] Failed to install skill: {ex}");
            }
        }

        private static void CopyDir(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var f in Directory.GetFiles(source))
                File.Copy(f, Path.Combine(target, Path.GetFileName(f)), overwrite: true);
            foreach (var d in Directory.GetDirectories(source))
                CopyDir(d, Path.Combine(target, Path.GetFileName(d)));
        }
    }
}
