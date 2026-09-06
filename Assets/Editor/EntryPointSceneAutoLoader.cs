#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;

// Resharper disable once CheckNamespace
namespace EditorSpace
{
    [InitializeOnLoad]
    public static class EntryPointSceneAutoLoader
    {
        private const string ToggleMenuPath = "Tools/Entry Point Scene Auto Loader/Toggle";
        private const string SetEntryMenuPath = "Tools/Entry Point Scene Auto Loader/Set Entry";
        private const string PrefKey = "EntryPointSceneAutoLoader.Enabled";
        
        public static bool Enabled { get; private set; }
        
        static EntryPointSceneAutoLoader()
        {
            SetEntry();
        }
        
        private static void SetEntry()
        {
            Enabled = EditorPrefs.GetBool(PrefKey, true);
            
            if (EditorBuildSettings.scenes.Length == 0 || !Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }
            
            EditorSceneManager.playModeStartScene = AssetDatabase
                .LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
        }
        
        [MenuItem(ToggleMenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            EditorPrefs.SetBool(PrefKey, Enabled);
            Menu.SetChecked(ToggleMenuPath, Enabled);
            SetEntry();
        }
    }
}

#endif