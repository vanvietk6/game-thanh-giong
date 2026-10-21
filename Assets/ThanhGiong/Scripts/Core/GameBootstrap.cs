using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Tự khởi động game khi bấm Play ở bất kỳ scene nào chưa có GameManager
    /// (ví dụ SampleScene mặc định). Camera và đèn sẵn có trong scene sẽ được tắt để tránh xung đột.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;

            foreach (var cam in Object.FindObjectsByType<Camera>())
                cam.gameObject.SetActive(false);
            foreach (var light in Object.FindObjectsByType<Light>())
                if (light.type == LightType.Directional) light.gameObject.SetActive(false);

            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }
}
