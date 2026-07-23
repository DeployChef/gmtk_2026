using TheyWillDescend.Gameplay.Buildings;
using UnityEngine;
using UnityEngine.UI;

namespace TheyWillDescend.UI.Buildings
{
    /// <summary>
    /// UI button that strikes a random house (tag "House") with VFX.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RandomStrikeButton : MonoBehaviour
    {
        private const string HouseTag = "House";

        [SerializeField] private GameObject lightningPrefab;
        [SerializeField] private float lightningLifetime = 3f;
        [SerializeField] private Vector3 lightningOffset = Vector3.zero;

        [SerializeField] private GameObject secondaryVfxPrefab;
        [SerializeField] private float secondaryVfxLifetime = 1f;
        [SerializeField] private Vector3 secondaryVfxOffset = Vector3.zero;

        [SerializeField] private GameObject tertiaryVfxPrefab;
        [SerializeField] private float tertiaryVfxLifetime = 5f;
        [SerializeField] private Vector3 tertiaryVfxOffset = Vector3.zero;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(StrikeRandomHouse);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(StrikeRandomHouse);
        }

        private void StrikeRandomHouse()
        {
            if (lightningPrefab == null)
            {
                Debug.LogWarning("[RandomStrikeButton] Lightning prefab is not assigned.");
                return;
            }

            var houses = GameObject.FindGameObjectsWithTag(HouseTag);
            if (houses.Length == 0)
            {
                Debug.LogWarning("[RandomStrikeButton] No objects with tag 'House' found.");
                return;
            }

            var target = houses[Random.Range(0, houses.Length)];
            var basePos = target.transform.position;

            SpawnVfx(lightningPrefab, basePos + lightningOffset, lightningLifetime);
            SpawnVfx(secondaryVfxPrefab, basePos + secondaryVfxOffset, secondaryVfxLifetime);
            SpawnVfx(tertiaryVfxPrefab, basePos + tertiaryVfxOffset, tertiaryVfxLifetime);
        }

        private static void SpawnVfx(GameObject prefab, Vector3 pos, float lifetime)
        {
            if (prefab == null)
                return;

            var vfx = Instantiate(prefab, pos, Quaternion.identity);
            Destroy(vfx, Mathf.Max(0.01f, lifetime));
        }
        }
    }
