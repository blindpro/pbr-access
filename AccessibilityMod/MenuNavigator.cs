using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccessibilityMod
{
    public class MenuNavigator : MonoBehaviour
    {
        private readonly List<Selectable> _currentSelectables = new List<Selectable>();
        private int _currentIndex = -1;
        private Selectable _lastSelected;
        private Canvas _lastActiveCanvas;
        private const float RepeatDelay = 0.4f;
        private const float RepeatRate = 0.15f;
        private float _nextRepeatTime;

        private void Update()
        {
            // Only run when a UI canvas is active and cursor is visible (menu mode)
            if (!Cursor.visible && Cursor.lockState == CursorLockMode.Locked)
                return;

            RefreshSelectablesIfNeeded();

            if (_currentSelectables.Count == 0)
                return;

            HandleNavigation();
            HandleActivation();
            DetectMouseSelection();
        }

        private void RefreshSelectablesIfNeeded()
        {
            // Rebuild the list when the active canvas changes or periodically
            // to pick up newly shown panels.
            var activeCanvas = GetTopActiveCanvas();
            if (activeCanvas != _lastActiveCanvas)
            {
                _lastActiveCanvas = activeCanvas;
                RebuildSelectablesList();
            }
        }

        private void RebuildSelectablesList()
        {
            _currentSelectables.Clear();
            _currentIndex = -1;

            if (_lastActiveCanvas == null) return;

            // Gather all interactable selectables under the active canvas,
            // sorted top-to-bottom by screen position (matching visual order).
            var all = _lastActiveCanvas.GetComponentsInChildren<Selectable>(false);
            _currentSelectables.AddRange(
                all.Where(s => s.IsInteractable() && s.gameObject.activeInHierarchy)
                   .OrderByDescending(s => GetScreenY(s))
            );

            if (_currentSelectables.Count > 0)
            {
                // If EventSystem already has something selected, sync to it
                var current = EventSystem.current?.currentSelectedGameObject;
                if (current != null)
                {
                    var sel = current.GetComponent<Selectable>();
                    int idx = _currentSelectables.IndexOf(sel);
                    if (idx >= 0)
                    {
                        _currentIndex = idx;
                        _lastSelected = sel;
                        return;
                    }
                }

                // Otherwise select the first item and announce it
                SetIndex(0);
            }
        }

        private void HandleNavigation()
        {
            int dir = 0;

            bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
            bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);
            bool upHeld = Input.GetKey(KeyCode.UpArrow);
            bool downHeld = Input.GetKey(KeyCode.DownArrow);

            if (upPressed)
            {
                dir = -1;
                _nextRepeatTime = Time.unscaledTime + RepeatDelay;
            }
            else if (downPressed)
            {
                dir = 1;
                _nextRepeatTime = Time.unscaledTime + RepeatDelay;
            }
            else if (upHeld && Time.unscaledTime >= _nextRepeatTime)
            {
                dir = -1;
                _nextRepeatTime = Time.unscaledTime + RepeatRate;
            }
            else if (downHeld && Time.unscaledTime >= _nextRepeatTime)
            {
                dir = 1;
                _nextRepeatTime = Time.unscaledTime + RepeatRate;
            }

            if (dir != 0 && _currentSelectables.Count > 0)
            {
                int newIndex = _currentIndex + dir;
                if (newIndex < 0) newIndex = _currentSelectables.Count - 1;
                if (newIndex >= _currentSelectables.Count) newIndex = 0;
                SetIndex(newIndex);
            }
        }

        private void HandleActivation()
        {
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
                return;

            if (_currentIndex < 0 || _currentIndex >= _currentSelectables.Count)
                return;

            var selectable = _currentSelectables[_currentIndex];
            ActivateSelectable(selectable);
        }

        private void DetectMouseSelection()
        {
            // If the user clicks on a UI element, sync our index to it
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current == null) return;

            var sel = current.GetComponent<Selectable>();
            if (sel == null || sel == _lastSelected) return;

            int idx = _currentSelectables.IndexOf(sel);
            if (idx >= 0 && idx != _currentIndex)
            {
                _currentIndex = idx;
                _lastSelected = sel;
                AnnounceSelectable(sel);
            }
        }

        private void SetIndex(int index)
        {
            _currentIndex = index;
            var selectable = _currentSelectables[index];
            _lastSelected = selectable;

            // Set EventSystem selection so the UI highlights correctly
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);

            selectable.Select();
            AnnounceSelectable(selectable);
        }

        private void AnnounceSelectable(Selectable selectable)
        {
            string label = GetSelectableLabel(selectable);
            string type = GetSelectableType(selectable);
            string state = GetSelectableState(selectable);

            string announcement = label;
            if (!string.IsNullOrEmpty(type))
                announcement += ", " + type;
            if (!string.IsNullOrEmpty(state))
                announcement += ", " + state;

            Plugin.Logger.LogDebug($"Announcing: {announcement}");
            ScreenReaderManager.Speak(announcement);
        }

        private static string GetSelectableLabel(Selectable selectable)
        {
            // Try to get the text from the component itself or its children
            // Check for direct text on the selectable's children
            var tmpText = selectable.GetComponentInChildren<TMPro.TMP_Text>(false);
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
                return tmpText.text.Trim();

            var uiText = selectable.GetComponentInChildren<Text>(false);
            if (uiText != null && !string.IsNullOrWhiteSpace(uiText.text))
                return uiText.text.Trim();

            // For dropdowns, read the caption
            var dropdown = selectable as Dropdown;
            if (dropdown != null && dropdown.captionText != null)
                return dropdown.captionText.text.Trim();

            // For sliders, look for a nearby label
            var slider = selectable as Slider;
            if (slider != null)
            {
                string nearby = FindNearbyLabel(selectable);
                if (!string.IsNullOrEmpty(nearby))
                    return nearby;
            }

            // For input fields, read placeholder or current text
            var inputField = selectable as InputField;
            if (inputField != null)
            {
                if (!string.IsNullOrWhiteSpace(inputField.text))
                    return inputField.text.Trim();
                if (inputField.placeholder != null)
                {
                    var placeholderText = inputField.placeholder.GetComponent<Text>();
                    if (placeholderText != null)
                        return placeholderText.text.Trim();
                }
            }

            // Try the GameObject name as last resort
            return CleanName(selectable.gameObject.name);
        }

        private static string GetSelectableType(Selectable selectable)
        {
            if (selectable is Button) return "button";
            if (selectable is Toggle) return "checkbox";
            if (selectable is Slider) return "slider";
            if (selectable is Dropdown) return "dropdown";
            if (selectable is InputField) return "text field";
            if (selectable is Scrollbar) return "scrollbar";
            return "";
        }

        private static string GetSelectableState(Selectable selectable)
        {
            var toggle = selectable as Toggle;
            if (toggle != null)
                return toggle.isOn ? "checked" : "not checked";

            var slider = selectable as Slider;
            if (slider != null)
            {
                float pct = (slider.value - slider.minValue) / (slider.maxValue - slider.minValue) * 100f;
                return $"{pct:0}%";
            }

            var dropdown = selectable as Dropdown;
            if (dropdown != null && dropdown.options.Count > 0)
            {
                string current = dropdown.options[dropdown.value].text;
                return $"{current}, {dropdown.value + 1} of {dropdown.options.Count}";
            }

            return "";
        }

        private static string FindNearbyLabel(Selectable selectable)
        {
            // Look for a Text or TMP_Text sibling or parent label
            var parent = selectable.transform.parent;
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child == selectable.transform) continue;
                var txt = child.GetComponent<Text>();
                if (txt != null && !string.IsNullOrWhiteSpace(txt.text))
                    return txt.text.Trim();
                var tmp = child.GetComponent<TMPro.TMP_Text>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                    return tmp.text.Trim();
            }
            return null;
        }

        private static string CleanName(string name)
        {
            // Convert "PlayButton" or "play_button" to "Play Button"
            var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            result = result.Replace("_", " ").Replace("(", "").Replace(")", "").Trim();
            return result;
        }

        private static void ActivateSelectable(Selectable selectable)
        {
            if (selectable is Button button)
            {
                button.onClick.Invoke();
                ScreenReaderManager.Speak("activated");
                return;
            }

            if (selectable is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                string state = toggle.isOn ? "checked" : "not checked";
                ScreenReaderManager.Speak(state);
                return;
            }

            if (selectable is Dropdown dropdown)
            {
                dropdown.Show();
                ScreenReaderManager.Speak("opened dropdown");
                return;
            }

            if (selectable is InputField inputField)
            {
                inputField.ActivateInputField();
                ScreenReaderManager.Speak("editing");
                return;
            }

            // For sliders, Enter doesn't do much but we can acknowledge
            if (selectable is Slider)
            {
                ScreenReaderManager.Speak("Use left and right arrows to adjust");
                return;
            }
        }

        private static float GetScreenY(Selectable s)
        {
            var rt = s.GetComponent<RectTransform>();
            if (rt == null) return 0;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            return screenPos.y;
        }

        private Canvas GetTopActiveCanvas()
        {
            // Find the topmost active canvas with interactable elements
            Canvas best = null;
            int bestOrder = int.MinValue;

            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                if (!canvas.isRootCanvas) continue;
                if (canvas.renderMode == RenderMode.WorldSpace) continue;

                // Check it has at least one interactable selectable
                var selectables = canvas.GetComponentsInChildren<Selectable>(false);
                bool hasInteractable = selectables.Any(s => s.IsInteractable() && s.gameObject.activeInHierarchy);
                if (!hasInteractable) continue;

                if (canvas.sortingOrder > bestOrder)
                {
                    bestOrder = canvas.sortingOrder;
                    best = canvas;
                }
            }
            return best;
        }
    }
}
