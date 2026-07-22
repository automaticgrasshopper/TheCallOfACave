using UnityEngine;
using UnityEngine.UI;

namespace TCC.UI
{
    /// <summary>Tabbed command dock that reserves stable screen space for future equipment.</summary>
    public class CommandDockView : MonoBehaviour
    {
        [SerializeField] private GameObject _buildPage;
        [SerializeField] private GameObject _equipmentPage;
        [SerializeField] private Button _buildTab;
        [SerializeField] private Button _equipmentTab;
        [SerializeField] private Image _buildTabImage;
        [SerializeField] private Image _equipmentTabImage;

        private void OnEnable()
        {
            if (_buildTab != null) _buildTab.onClick.AddListener(ShowBuild);
            if (_equipmentTab != null) _equipmentTab.onClick.AddListener(ShowEquipment);
            ShowBuild();
        }

        private void OnDisable()
        {
            if (_buildTab != null) _buildTab.onClick.RemoveListener(ShowBuild);
            if (_equipmentTab != null) _equipmentTab.onClick.RemoveListener(ShowEquipment);
        }

        public void Configure(GameObject buildPage, GameObject equipmentPage, Button buildTab,
            Button equipmentTab, Image buildTabImage, Image equipmentTabImage)
        {
            _buildPage = buildPage;
            _equipmentPage = equipmentPage;
            _buildTab = buildTab;
            _equipmentTab = equipmentTab;
            _buildTabImage = buildTabImage;
            _equipmentTabImage = equipmentTabImage;
        }

        private void ShowBuild() => Show(true);
        private void ShowEquipment() => Show(false);

        private void Show(bool build)
        {
            if (_buildPage != null) _buildPage.SetActive(build);
            if (_equipmentPage != null) _equipmentPage.SetActive(!build);
            if (_buildTabImage != null) _buildTabImage.color = build ? Active() : Idle();
            if (_equipmentTabImage != null) _equipmentTabImage.color = build ? Idle() : Active();
        }

        private static Color Active() => new Color(.08f, .2f, .18f, 1f);
        private static Color Idle() => new Color(.025f, .045f, .05f, .95f);
    }
}
