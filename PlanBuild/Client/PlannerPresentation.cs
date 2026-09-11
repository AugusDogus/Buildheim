using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class PlannerPresentation
    {
        private readonly GameObject root;
        private readonly RectTransform rect;
        private readonly CanvasGroup input;
        private readonly MenuSlide slide = new MenuSlide();
        private readonly float openDuration = 0.5f / 3f;
        private readonly float closeDuration = 0.5f / 3f;
        public bool Present => root.activeSelf;

        public PlannerPresentation(GameObject root)
        {
            this.root = root;
            rect = root.GetComponent<RectTransform>();
            input = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
            // Valheim's inventory_show/hide clips slide linearly for 0.5 s;
            // Inventory_screen plays those states at speed 3 (Valheim 1.0.7).
            var inventory = InventoryGui.instance;
            var animator = inventory ? inventory.GetComponent<Animator>() : null;
            if (animator && animator.runtimeAnimatorController)
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == "inventory_show") openDuration = clip.length / 3f;
                    if (clip.name == "inventory_hide") closeDuration = clip.length / 3f;
                }
            Hide();
        }

        public void Update(bool visible, Rect canvas)
        {
            slide.Advance(visible, Time.unscaledDeltaTime, visible ? openDuration : closeDuration);
            root.SetActive(visible || slide.Amount > 0);
            // Closing visuals must not keep eating mouse input or allow another button click.
            input.interactable = input.blocksRaycasts = visible;
            float scale = Mathf.Max(0.3f, Mathf.Min(1, Mathf.Min((canvas.width - 32) / 700f, (canvas.height - 32) / 640f)));
            root.transform.localScale = Vector3.one * scale;
            float offscreen = -(canvas.width + 700 * scale) / 2 - 16;
            rect.anchoredPosition = new Vector2(offscreen * (1 - slide.Amount), 0);
        }

        public static void PlaySound(bool visible)
        {
            if (!InventoryGui.instance || !Player.m_localPlayer) return;
            var effects = visible ? InventoryGui.instance.m_openInventoryEffects : InventoryGui.instance.m_closeInventoryEffects;
            effects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
        }

        public void Hide()
        {
            slide.Reset();
            if (!root) return;
            input.interactable = input.blocksRaycasts = false;
            root.SetActive(false);
        }
    }
}
