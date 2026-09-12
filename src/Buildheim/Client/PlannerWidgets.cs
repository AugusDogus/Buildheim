using System;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Client
{
    // Shared Valheim assets, with coordinates measured from a panel's top left.
    internal static class PlannerWidgets
    {
        public static readonly Color Gold = GUIManager.Instance.ValheimOrange;
        private static readonly Vector2 TopLeft = new Vector2(0, 1);

        public static void Place(GameObject obj, float x, float y, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = TopLeft;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static GameObject Group(Transform parent, float x, float y, float width, float height)
        {
            var obj = new GameObject("Planner section", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Place(obj, x, y, width, height);
            return obj;
        }

        public static Text Label(Transform parent, string text, float x, float y, float width, float height, int size = 18, bool heading = false)
        {
            var gui = GUIManager.Instance;
            var obj = gui.CreateText(text, parent, TopLeft, TopLeft, Vector2.zero,
                heading ? gui.AveriaSerifBold : gui.AveriaSerif, size,
                heading ? Gold : GUIManager.Instance.ValheimBeige, true, Color.black, width, height, false);
            Place(obj, x, y, width, height);
            var label = obj.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.supportRichText = false;
            return label;
        }

        public static Button Button(Transform parent, string text, float x, float y, float width, Action click, float height = 36)
        {
            var obj = GUIManager.Instance.CreateButton(text, parent, TopLeft, TopLeft, Vector2.zero, width, height);
            Place(obj, x, y, width, height);
            var button = obj.GetComponent<Button>();
            var colors = button.colors;
            colors.disabledColor = new Color(0.8f, 0.65f, 0.35f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = obj.GetComponentInChildren<Text>();
            label.fontSize = 18;
            label.supportRichText = false;
            button.onClick.AddListener(() => click());
            return button;
        }

        public static InputField Input(Transform parent, string placeholder, float x, float y, float width)
        {
            var obj = GUIManager.Instance.CreateInputField(parent, TopLeft, TopLeft, Vector2.zero,
                InputField.ContentType.Standard, placeholder, 18, width, 36);
            Place(obj, x, y, width, 36);
            var input = obj.GetComponent<InputField>();
            input.characterLimit = 80;
            return input;
        }

        public static Slider Slider(Transform parent, float x, float y, float width, float min, float max, float value, Action<float> change)
        {
            var obj = DefaultControls.CreateSlider(GUIManager.Instance.ValheimControlResources);
            obj.transform.SetParent(parent, false);
            Place(obj, x, y, width, 30);
            var slider = obj.GetComponent<Slider>();
            GUIManager.Instance.ApplySliderStyle(slider);
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(number => change(number));
            return slider;
        }

        public static ScrollRect Scroll(Transform parent, float x, float y, float width, float height)
        {
            var obj = DefaultControls.CreateScrollView(GUIManager.Instance.ValheimControlResources);
            obj.transform.SetParent(parent, false);
            Place(obj, x, y, width, height);
            var scroll = obj.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            scroll.horizontalScrollbar.gameObject.SetActive(false);
            scroll.horizontalScrollbar = null;
            scroll.viewport.offsetMin = new Vector2(0, 0);
            scroll.viewport.offsetMax = new Vector2(-20, 0);
            obj.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);
            GUIManager.Instance.ApplyScrollbarStyle(scroll.verticalScrollbar);
            scroll.content.anchorMin = new Vector2(0, 1);
            scroll.content.anchorMax = new Vector2(1, 1);
            scroll.content.pivot = new Vector2(0, 1);
            return scroll;
        }

        public static void Clear(Transform parent)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
