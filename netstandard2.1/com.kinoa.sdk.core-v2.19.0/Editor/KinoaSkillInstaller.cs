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
    ///     <para>Hard-overwrites on every package version change. Local edits are lost by design.</para>
    /// </summary>
    [InitializeOnLoad]
    internal static class KinoaSkillInstaller
    {
        private const string PackageName = "com.kinoa.sdk.core";
        private const string SkillPath = ".claude/skills/kinoa";
        private const string VersionKey = "Kinoa.SdkCore.InstalledSkillVersion";

        static KinoaSkillInstaller() => EditorApplication.delayCall += Run;

        [MenuItem("Tools/Kinoa/Reinstall AI Integration Skill")]
        private static void ForceReinstall()
        {
            EditorPrefs.DeleteKey(VersionKey);
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

            if (EditorPrefs.GetString(VersionKey) == pkg.version)
            {
                return; // Already up to date.
            }

            var source = Path.Combine(pkg.resolvedPath, SkillPath);
            var target = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SkillPath);

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Kinoa] Skill source not found at \"{source}\".");
                return;
            }

            try
            {
                if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
                CopyDir(source, target);
                EditorPrefs.SetString(VersionKey, pkg.version);
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
