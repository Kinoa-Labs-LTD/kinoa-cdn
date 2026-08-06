#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

public class BuildPostProcessor
{
    private const string PackageName = "com.kinoa.sdk.push";
    private const string ServiceExtensionTargetName = "KinoaNotificationServiceExtension";
    private const string ServiceExtensionFilename = "KinoaNotificationService.swift";
    private static readonly string PluginLibrariesPath = Path.Combine(PackageName, "Runtime", "Plugins", "iOS");
    private static readonly string PluginFilesPath = Path.Combine("Packages", PluginLibrariesPath); 
    private static readonly string SoundsPath = Path.Combine("Kinoa", "Sounds");
    private static readonly List<string> SoundsExtensions = new List<string>() { ".aiff", ".caf", ".wav" };

    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS)
        {
            var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            var targetGUID = proj.GetUnityMainTargetGuid();

            AddSoundFiles(proj, pathToBuiltProject);

            var notificationServicePlistPath = ExtensionCreatePlist(pathToBuiltProject);
            
            var notificationServiceTarget = proj.AddAppExtension(
                targetGUID,
                ServiceExtensionTargetName,
                PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS) + "." + ServiceExtensionTargetName,
                notificationServicePlistPath);
            
            ExtensionAddSourceFiles(pathToBuiltProject);
            
            proj.AddFileToBuild(notificationServiceTarget,
                proj.AddFile(Path.Combine(ServiceExtensionTargetName, ServiceExtensionFilename),
                    Path.Combine(ServiceExtensionTargetName, ServiceExtensionFilename)));
            
            proj.AddFile(Path.Combine(ServiceExtensionTargetName, "Info.plist"),
                Path.Combine(ServiceExtensionTargetName, "Info.plist"));

            proj.SetBuildProperty(notificationServiceTarget, "TARGETED_DEVICE_FAMILY", "1,2");
            proj.SetBuildProperty(notificationServiceTarget, "IPHONEOS_DEPLOYMENT_TARGET", "10.0");
            proj.SetBuildProperty(notificationServiceTarget, "SWIFT_VERSION", "5.0");
            proj.SetBuildProperty(notificationServiceTarget, "ARCHS", "$(ARCHS_STANDARD)");
            proj.SetBuildProperty(notificationServiceTarget, "DEVELOPMENT_TEAM",
                PlayerSettings.iOS.appleDeveloperTeamID);

            proj.WriteToFile(projPath);
        }
    }
    
    private static string ExtensionCreatePlist(string pathToBuiltProject) {
        var extensionPath = Path.Combine(pathToBuiltProject, ServiceExtensionTargetName);
        Directory.CreateDirectory(extensionPath);

        var plistPath     = Path.Combine(extensionPath, "Info.plist");
        var notificationServicePlist = new PlistDocument();
        notificationServicePlist.ReadFromFile(Path.Combine(PluginFilesPath, "Info.plist"));
        notificationServicePlist.root.SetString("CFBundleShortVersionString", PlayerSettings.bundleVersion);
        notificationServicePlist.root.SetString("CFBundleVersion", PlayerSettings.iOS.buildNumber);
        notificationServicePlist.WriteToFile(plistPath);
        
        return plistPath;
    }
    
    private static void ExtensionAddSourceFiles(string pathToBuiltProject) {
        var sourcePath       = Path.Combine(PluginFilesPath, ServiceExtensionFilename);
        var destPathRelative = Path.Combine(ServiceExtensionTargetName, ServiceExtensionFilename);

        var destPath = Path.Combine(pathToBuiltProject, destPathRelative);
        if (!File.Exists(destPath))
            FileUtil.CopyFileOrDirectory(sourcePath.Replace("\\", "/"), destPath.Replace("\\", "/"));
    }
    
    private static void AddSoundFiles(PBXProject proj, string pathToBuiltProject) {
        try
        {
            var sourcePath = Path.Combine(Application.streamingAssetsPath, SoundsPath);
            if (Directory.Exists(sourcePath))
            {
                var soundsList = Directory.GetFiles(sourcePath);
                foreach (var soundFile in soundsList)
                {
                    if (SoundsExtensions.Contains(Path.GetExtension(soundFile)))
                    {
                        var fileName = Path.GetFileName(soundFile);
                        var dest = Path.Combine(pathToBuiltProject, fileName);
                        
                        FileUtil.CopyFileOrDirectory(soundFile.Replace("\\", "/"), dest.Replace("\\", "/"));
                        var fileGuid = proj.AddFile(fileName, fileName, PBXSourceTree.Source);
						proj.AddFileToBuild(proj.GetUnityMainTargetGuid(), fileGuid);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"[KinoaBuildPostProcessor] Exception: {e.Message}");
        }
    }

}
#endif
