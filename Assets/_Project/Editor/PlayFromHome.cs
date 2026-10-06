using UnityEditor;
using UnityEditor.SceneManagement;

namespace Match3.EditorTools
{
    /// <summary>
    /// In the editor, Play normally starts the scene that is open, not the first scene of the build.
    /// This makes Play always start on the Home scene (like the real game), whatever scene you are editing.
    /// Switch it off with Tools > Match3 > Play From Home when you want to test the Game scene on its own.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromHome
    {
        private const string MenuPath = "Tools/Match3/Play From Home";
        private const string PrefKey = "Match3.PlayFromHome";
        private const string HomeScenePath = "Assets/_Project/Scenes/Home.unity";

        static PlayFromHome()
        {
            Apply(); // runs after every script reload, so the setting is always in place
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true); // on by default
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled); // the checkmark in the menu
            return true;
        }

        private static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            // Right after the Library folder was deleted the scene is not imported yet, so the load gives null and
            // Play would start the open (maybe empty) scene. Try again on the next editor tick until the scene exists.
            SceneAsset home = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeScenePath);
            if (home == null)
            {
                EditorApplication.delayCall += Apply;
                return;
            }

            EditorSceneManager.playModeStartScene = home;
        }
    }
}
