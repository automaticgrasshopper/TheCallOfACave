using UnityEngine;
using UnityEngine.UI;

namespace TCC.UI
{
    /// <summary>Tabbed command dock that reserves stable screen space for future equipment.</summary>
    public class CommandDockView : MonoBehaviour
    {
        [SerializeField] private GameObject _buildPage;
        [SerializeField] private GameObject _researchPage;
        [SerializeField] private GameObject _equipmentPage;
        [SerializeField] private Button _buildTab;
        [SerializeField] private Button _researchTab;
        [SerializeField] private Button _equipmentTab;
        [SerializeField] private Image _buildTabImage;
        [SerializeField] private Image _researchTabImage;
        [SerializeField] private Image _equipmentTabImage;

        private void OnEnable()
        {
            if (_equipmentPage != null && _equipmentPage.GetComponent<EquipmentSynthesisView>() == null)
                _equipmentPage.AddComponent<EquipmentSynthesisView>();
            if (_buildTab != null) _buildTab.onClick.AddListener(ShowBuild);
            if (_researchTab != null) _researchTab.onClick.AddListener(ShowResearch);
            if (_equipmentTab != null) _equipmentTab.onClick.AddListener(ShowEquipment);
            ShowBuild();
        }

        private void OnDisable()
        {
            if (_buildTab != null) _buildTab.onClick.RemoveListener(ShowBuild);
            if (_researchTab != null) _researchTab.onClick.RemoveListener(ShowResearch);
            if (_equipmentTab != null) _equipmentTab.onClick.RemoveListener(ShowEquipment);
        }

        public void Configure(GameObject buildPage, GameObject researchPage, GameObject equipmentPage,
            Button buildTab, Button researchTab, Button equipmentTab, Image buildTabImage,
            Image researchTabImage, Image equipmentTabImage)
        {
            _buildPage = buildPage;
            _researchPage = researchPage;
            _equipmentPage = equipmentPage;
            _buildTab = buildTab;
            _researchTab = researchTab;
            _equipmentTab = equipmentTab;
            _buildTabImage = buildTabImage;
            _researchTabImage = researchTabImage;
            _equipmentTabImage = equipmentTabImage;
        }

        private void ShowBuild() => Show(0);
        private void ShowResearch() => Show(1);
        private void ShowEquipment() => Show(2);

        private void Show(int page)
        {
            if (_buildPage != null) _buildPage.SetActive(page == 0);
            if (_researchPage != null) _researchPage.SetActive(page == 1);
            if (_equipmentPage != null) _equipmentPage.SetActive(page == 2);
            if (_buildTabImage != null) _buildTabImage.color = page == 0 ? Active() : Idle();
            if (_researchTabImage != null) _researchTabImage.color = page == 1 ? Active() : Idle();
            if (_equipmentTabImage != null) _equipmentTabImage.color = page == 2 ? Active() : Idle();
        }

        private static Color Active() => new Color(.08f, .2f, .18f, 1f);
        private static Color Idle() => new Color(.025f, .045f, .05f, .95f);
    }
}
