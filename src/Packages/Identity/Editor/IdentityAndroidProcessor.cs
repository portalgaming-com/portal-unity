#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Portal.Identity.Editor
{
    class IdentityAndroidProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 0; } }
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Debug.Log("MyCustomBuildProcessor.OnPostGenerateGradleAndroidProject at path " + path);

            // Find the location of the files
            string identityWebFilesDir = Path.GetFullPath("Packages/com.portal.identity/Runtime/Resources");
            if (!Directory.Exists(identityWebFilesDir))
            {
                Debug.LogError("The Identity files directory doesn't exist!");
                return;
            }

            FileHelpers.CopyDirectory(identityWebFilesDir, $"{path}/src/main/assets/PortalSDK/Runtime/Identity");
            Debug.Log($"Sucessfully copied Identity files");

            AddUseAndroidX(path);
        }

        private void AddUseAndroidX(string path)
        {
            var parentDir = Directory.GetParent(path).FullName;
            var gradlePath = parentDir + "/gradle.properties";

            if (!File.Exists(gradlePath))
                throw new Exception("gradle.properties does not exist");

            var text = File.ReadAllText(gradlePath);

            text += "\nandroid.useAndroidX=true\nandroid.enableJetifier=true";

            File.WriteAllText(gradlePath, text);
        }
    }
}

#endif