using UnityEngine;

namespace TheyWillDescend.UI.Dialogue
{
    /// <summary>
    /// Scene-assembled pyramid strike VFX for the opening (lightning + fire look).
    /// Place inactive VFX roots under the pyramid (or assign prefabs). Sequencer calls <see cref="Play"/>.
    /// </summary>
    public sealed class IntroPyramidStrikeVfx : MonoBehaviour
    {
        private const int SortingOrder = 5;

        [Tooltip("Where prefabs spawn. Defaults to this transform.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Scene VFX (assembled in hierarchy, start inactive)")]
        [SerializeField] private GameObject[] sceneVfxRoots;

        [Header("Optional prefabs (spawned at Play)")]
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private float prefabLifetime = 3f;
        [SerializeField] private Vector3 prefabOffset;

        private void Awake()
        {
            SetSceneVfxActive(false);
        }

        public void Play()
        {
            var origin = spawnPoint != null ? spawnPoint : transform;

            if (sceneVfxRoots != null)
            {
                for (var i = 0; i < sceneVfxRoots.Length; i++)
                {
                    var root = sceneVfxRoots[i];
                    if (root == null)
                        continue;

                    root.SetActive(true);
                    RestartParticles(root);
                }
            }

            if (prefabs == null)
                return;

            var life = Mathf.Max(0.01f, prefabLifetime);
            for (var i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null)
                    continue;

                var instance = Instantiate(prefab, origin.position + prefabOffset, Quaternion.identity);
                BoostSorting(instance);
                RestartParticles(instance);
                Destroy(instance, life);
            }
        }

        public void Hide()
        {
            SetSceneVfxActive(false);
        }

        private void SetSceneVfxActive(bool active)
        {
            if (sceneVfxRoots == null)
                return;

            for (var i = 0; i < sceneVfxRoots.Length; i++)
            {
                if (sceneVfxRoots[i] != null)
                    sceneVfxRoots[i].SetActive(active);
            }
        }

        private static void RestartParticles(GameObject root)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                var main = ps.main;
                main.useUnscaledTime = true;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private static void BoostSorting(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sortingOrder = SortingOrder;
        }
    }
}
