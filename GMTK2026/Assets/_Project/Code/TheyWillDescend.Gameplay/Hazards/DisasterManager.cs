using TheyWillDescend.Core.Hazards;
using UnityEngine;
using VContainer;

namespace TheyWillDescend.Gameplay.Hazards
{
    /// <summary>
    /// Scene façade for disasters: Inspector VFX + delegates strike effects to <see cref="IThunderService"/>.
    /// </summary>
    public sealed class DisasterManager : MonoBehaviour, IDisasterManager
    {
        private const string HouseTag = "House";
        private const int VfxSortingOrder = 5;

        [SerializeField] private float disableDuration = 10f;

        [SerializeField] private GameObject lightningPrefab;
        [SerializeField] private float lightningLifetime = 3f;
        [SerializeField] private Vector3 lightningOffset = Vector3.zero;

        [SerializeField] private GameObject secondaryVfxPrefab;
        [SerializeField] private float secondaryVfxLifetime = 1f;
        [SerializeField] private Vector3 secondaryVfxOffset = Vector3.zero;

        [SerializeField] private GameObject tertiaryVfxPrefab;
        [SerializeField] private float tertiaryVfxLifetime = 5f;
        [SerializeField] private Vector3 tertiaryVfxOffset = Vector3.zero;

        private IThunderService _thunder;

        [Inject]
        public void Construct(IThunderService thunder)
        {
            _thunder = thunder;
        }

        public bool TryStrikeRandomHouse()
        {
            if (lightningPrefab == null)
            {
                Debug.LogWarning("[DisasterManager] Lightning prefab is not assigned.");
                return false;
            }

            var houses = GameObject.FindGameObjectsWithTag(HouseTag);
            if (houses.Length == 0)
            {
                Debug.LogWarning("[DisasterManager] No objects with tag 'House' found.");
                return false;
            }

            var target = houses[Random.Range(0, houses.Length)];
            var basePos = target.transform.position;

            SpawnVfx(lightningPrefab, basePos + lightningOffset, lightningLifetime);
            SpawnVfx(secondaryVfxPrefab, basePos + secondaryVfxOffset, secondaryVfxLifetime);
            SpawnVfx(tertiaryVfxPrefab, basePos + tertiaryVfxOffset, tertiaryVfxLifetime);

            _thunder?.ApplyStrike(target, disableDuration);
            return true;
        }

        private static void SpawnVfx(GameObject prefab, Vector3 pos, float lifetime)
        {
            if (prefab == null)
                return;

            var vfx = Instantiate(prefab, pos, Quaternion.identity);

            foreach (var renderer in vfx.GetComponentsInChildren<Renderer>())
                renderer.sortingOrder = VfxSortingOrder;

            Destroy(vfx, Mathf.Max(0.01f, lifetime));
        }
    }
}
