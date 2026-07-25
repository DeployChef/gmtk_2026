using UnityEngine;

namespace TheyWillDescend.Main.Display
{
    static class DisplayResolutionEnforcer
    {
        const int Width = 1920;
        const int Height = 1080;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            // WebGL size is controlled by the HTML template CSS — Screen.SetResolution
            // forces a fixed canvas size and breaks letterboxing on 16:9.
#if UNITY_STANDALONE && !UNITY_EDITOR
            Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
#endif
        }
    }
}
