using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Wiring for one generated customizer row. Lives on scene objects created by
    /// UmaAvatarCustomizer's "Generate Panel Now" so the panel can be freely tweaked
    /// in the editor (move/resize/style) while the runtime binds by component, not path.
    /// </summary>
    public class UmaCustomizerRow : MonoBehaviour
    {
        public enum RowRole { Race, WardrobeSlot, Save, AvatarSystem }

        public RowRole role;
        [Tooltip("UMA wardrobe slot this row controls (WardrobeSlot rows only).")]
        public string slotName;
        public Button previousButton;
        public Button nextButton;
        public TMP_Text titleLabel;
        public TMP_Text valueLabel;
        public Image thumbImage;
    }
}
