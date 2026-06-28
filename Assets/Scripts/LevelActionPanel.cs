using UnityEngine;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelActionPanel : MonoBehaviour
    {
        public Button clearButton;
        public Button submitButton;
        public Button backButton;

        public void BindDefaults()
        {
            clearButton = clearButton != null ? clearButton : FindButton("ClearButton");
            submitButton = submitButton != null ? submitButton : FindButton("SubmitButton");
            backButton = backButton != null ? backButton : FindButton("BackButton");
        }

        public void Initialize(KillerMineDokuLevelController controller)
        {
            BindDefaults();
            clearButton?.onClick.AddListener(controller.ClearMarks);
            submitButton?.onClick.AddListener(controller.Submit);
            backButton?.onClick.AddListener(controller.BackToLevelSelect);
        }

        private Button FindButton(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<Button>() : null;
        }
    }
}
