using TheyWillDescend.Core.Hazards;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TheyWillDescend.UI.Buildings
{
    /// <summary>
    /// UI button → <see cref="IDisasterManager.TryStrikeRandomHouse"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RandomStrikeButton : MonoBehaviour
    {
        private Button _button;
        private IDisasterManager _disasters;

        [Inject]
        public void Construct(IDisasterManager disasters)
        {
            _disasters = disasters;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (_disasters == null)
            {
                Debug.LogWarning("[RandomStrikeButton] IDisasterManager is not injected.");
                return;
            }

            _disasters.TryStrikeRandomHouse();
        }
    }
}
