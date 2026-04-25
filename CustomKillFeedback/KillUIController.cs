using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// UI 控制器：负责展示击杀反馈图标/文本，并与原生 HUDManager 对齐。
    /// 动画逻辑参考 CFKillFeedback，实现落下-停留-淡出三段曲线。
    /// </summary>
    internal sealed class KillUIController : MonoBehaviour
    {
        private const float DropDuration = 0.1f;
        private const float FadeDuration = 0.25f;
        private static readonly Vector2 IconPosDrop = new Vector2(0.5f, 0.12f);
        private static readonly Vector2 IconPosStay = new Vector2(0.5f, 0.22f);
        private static readonly Vector3 IconScaleDrop = new Vector3(1.35f, 1.35f, 1f);
        private static readonly Vector3 IconScaleStay = new Vector3(1f, 1f, 1f);
        private static readonly Vector2 BaseResolution = new Vector2(1920f, 1080f);

        private static KillUIController _instance;

        public static KillUIController EnsureInstance()
        {
            if (_instance != null) return _instance;

            var existing = FindObjectOfType<KillUIController>(includeInactive: true);
            if (existing != null)
            {
                _instance = existing;
                _instance.BuildUIIfNeeded();
                return _instance;
            }

            var go = new GameObject("[DCS] KillUI");
            var controller = go.AddComponent<KillUIController>();
            controller.BuildUIIfNeeded();
            _instance = controller;
            return controller;
        }

        public static void DestroyInstance()
        {
            if (_instance == null) return;
            try
            {
                if (_instance != null)
                {
                    Destroy(_instance.gameObject);
                }
            }
            finally
            {
                _instance = null;
            }
        }

        private RectTransform _rect;
        private CanvasGroup _group;
        private Image _icon;
        private RectTransform _iconRect;
        private TextMeshProUGUI _label;
        private RectTransform _labelRect;
        private HUDManager _hud;

        private float _lastTriggerTime;
        private bool _isAnimating;
        private Sprite _pendingSprite;
        private string _pendingLabel;
        private bool _iconEnabled;

        private bool _uiBuilt;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            BuildUIIfNeeded();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void ApplyConfig()
        {
            BuildUIIfNeeded();

            if (_label != null)
            {
                _label.fontSize = KillFeedbackConfig.FontSize;
                _label.color = KillFeedbackConfig.TextColor;
                _label.enabled = KillFeedbackConfig.ShowText && !string.IsNullOrEmpty(_label.text);
            }
        }

        public void ShowKill(KillEventContext context)
        {
            BuildUIIfNeeded();
            if (_group == null) return;

            _pendingLabel = ComposeLabel(context);
            _pendingSprite = KillIconLibrary.Resolve(context);
            _iconEnabled = _pendingSprite != null && KillFeedbackConfig.UseIcons;

            ActivateDisplay();
        }

        private void LateUpdate()
        {
            BuildUIIfNeeded();
            TryAttachToHud();
            TickAnimation();
        }

        private void BuildUIIfNeeded()
        {
            if (_uiBuilt) return;

            _rect = gameObject.GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = Vector2.zero;

            _group = gameObject.GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(transform, false);
            _icon = iconGO.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.enabled = false;
            _iconRect = _icon.rectTransform;
            _iconRect.anchorMin = _iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            _iconRect.pivot = new Vector2(0.5f, 0.5f);
            _iconRect.anchoredPosition = Vector2.zero;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(transform, false);
            _label = textGO.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
            _label.text = string.Empty;
            _labelRect = _label.rectTransform;
            _labelRect.anchorMin = _labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _labelRect.pivot = new Vector2(0.5f, 0.5f);
            _labelRect.anchoredPosition = new Vector2(0f, -48f);
            _labelRect.sizeDelta = new Vector2(600f, 160f);

            ApplyConfig();
            TryAttachToHud();

            _uiBuilt = true;
        }

        private void TryAttachToHud()
        {
            if (_hud != null && _hud) return;

            var hud = FindObjectOfType<HUDManager>();
            if (hud == null) return;

            _hud = hud;
            transform.SetParent(hud.transform, false);
            _rect.anchoredPosition = Vector2.zero;
        }

        private void ActivateDisplay()
        {
            _lastTriggerTime = Time.unscaledTime;
            _isAnimating = true;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            if (_label != null)
            {
                _label.text = _pendingLabel ?? string.Empty;
                _label.enabled = KillFeedbackConfig.ShowText && !string.IsNullOrEmpty(_label.text);
            }

            if (_icon != null)
            {
                if (KillFeedbackConfig.UseIcons && _pendingSprite != null)
                {
                    _icon.sprite = _pendingSprite;
                    _icon.enabled = true;
                    _iconEnabled = true;
                }
                else
                {
                    _icon.sprite = null;
                    _icon.enabled = false;
                    _iconEnabled = false;
                }
            }
        }

        private void TickAnimation()
        {
            if (!_isAnimating || _group == null) return;

            float elapsed = Time.unscaledTime - _lastTriggerTime;
            float stayDuration = Mathf.Max(0f, KillFeedbackConfig.DisplayDuration - DropDuration - FadeDuration);
            float targetAlpha = Mathf.Clamp01(KillFeedbackConfig.IconAlpha);

            var parentRect = _rect.parent as RectTransform;
            Vector2 parentSize = parentRect ? parentRect.rect.size : new Vector2(Screen.width, Screen.height);
            float scaleFactor = parentSize == Vector2.zero
                ? 1f
                : Mathf.Min(parentSize.x / BaseResolution.x, parentSize.y / BaseResolution.y);
            Vector3 stayScale = IconScaleStay * scaleFactor;
            Vector3 dropScale = IconScaleDrop * scaleFactor;
            Vector2 dropPos = ToAnchored(parentSize, IconPosDrop);
            Vector2 stayPos = ToAnchored(parentSize, IconPosStay);

            if (elapsed < DropDuration)
            {
                float t = Mathf.Clamp01(elapsed / DropDuration);
                _group.alpha = Mathf.Lerp(0f, targetAlpha, t);
                _rect.localScale = Vector3.Lerp(dropScale, stayScale, t);
                _rect.anchoredPosition = Vector2.Lerp(dropPos, stayPos, t);
            }
            else if (elapsed < DropDuration + stayDuration)
            {
                _group.alpha = targetAlpha;
                _rect.localScale = stayScale;
                _rect.anchoredPosition = stayPos;
            }
            else if (elapsed < DropDuration + stayDuration + FadeDuration)
            {
                float t = Mathf.Clamp01((elapsed - DropDuration - stayDuration) / FadeDuration);
                _group.alpha = Mathf.Lerp(targetAlpha, 0f, t);
                _rect.localScale = stayScale;
                _rect.anchoredPosition = stayPos;
            }
            else
            {
                _group.alpha = 0f;
                _rect.localScale = stayScale;
                _rect.anchoredPosition = stayPos;
                _isAnimating = false;
            }
        }

        private static Vector2 ToAnchored(Vector2 parentSize, Vector2 percent)
        {
            if (parentSize == Vector2.zero)
            {
                parentSize = new Vector2(Screen.width, Screen.height);
            }
            return new Vector2(
                (percent.x - 0.5f) * parentSize.x,
                (percent.y - 0.5f) * parentSize.y
            );
        }

        private static string ComposeLabel(KillEventContext context)
        {
            if (context.IsHeadshot && context.Streak <= 1) return "HEADSHOT";
            if (context.IsHeadshot && context.Streak > 1) return $"HEADSHOT x{context.Streak}";
            if (context.Streak >= 2) return $"{context.Streak} KILL";
            return "KILL";
        }
    }
}
