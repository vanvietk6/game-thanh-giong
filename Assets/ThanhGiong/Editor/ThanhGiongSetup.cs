using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThanhGiong.EditorTools
{
    /// <summary>Công cụ trong Editor: tạo scene game và đặt làm scene đầu tiên khi build.</summary>
    public static class ThanhGiongSetup
    {
        const string ScenePath = "Assets/Scenes/ThanhGiong.unity";

        const string MaterialPath = "Assets/ThanhGiong/Resources/" + MatLib.BaseMaterialName + ".mat";

        /// <summary>Tạo material gốc dùng shader URP Lit trong Resources để shader không bị loại khi build.</summary>
        public static void EnsureBaseMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return;
            if (!AssetDatabase.IsValidFolder("Assets/ThanhGiong/Resources"))
                AssetDatabase.CreateFolder("Assets/ThanhGiong", "Resources");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { name = MatLib.BaseMaterialName };
            AssetDatabase.CreateAsset(mat, MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[THANHGIONG] Đã tạo material " + MaterialPath);
        }

        [MenuItem("Thanh Giong/Tao lai Scene game")]
        public static void CreateScene()
        {
            EnsureBaseMaterial();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameManager").AddComponent<GameManager>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene> { new(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.productName = "Thanh Giong";
            AssetDatabase.SaveAssets();
            Debug.Log("[THANHGIONG] Đã tạo scene " + ScenePath);
        }

        [MenuItem("Thanh Giong/Build ban Windows")]
        public static void BuildWindows()
        {
            EnsureBaseMaterial();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/ThanhGiong.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[THANHGIONG] Build: " + report.summary.result + " -> " + options.locationPathName);
        }
    }
}
