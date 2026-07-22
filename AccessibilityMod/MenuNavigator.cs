using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccessibilityMod
{
    public class MenuNavigator
    {
        private readonly List<Selectable> _currentSelectables = new List<Selectable>();
        private int _currentIndex = -1;
        private Selectable _lastSelected;
        private const float RepeatDelay = 0.4f;
        private const float RepeatRate = 0.15f;
        private float _nextRepeatTime;
        private float _nextRescanTime;
        private const float RescanInterval = 1.0f;
        private int _lastSelectableCount;
        private Transform _scopeRoot;

        public void Tick()
        {
            // The loot list and the inventory borrow Up, Down and Enter while they
            // are open; HUD buttons must not answer them at the same time.
            if (LootMenu.IsOpen || InventoryMenu.IsOpen) return;

            // Periodically rescan for selectables
            if (Time.unscaledTime >= _nextRescanTime)
            {
                _nextRescanTime = Time.unscaledTime + RescanInterval;
                RebuildSelectablesList();
            }

            if (_currentSelectables.Count == 0)
                return;

            HandleNavigation();
            HandleActivation();
            DetectExternalSelection();
        }

        private void RebuildSelectablesList()
        {
            _currentSelectables.Clear();

            var candidates = new List<Selectable>();
            var allSelectables = UnityEngine.Object.FindObjectsOfType<Selectable>();

            foreach (var s in allSelectables)
            {
                if (s == null) continue;
                if (!s.gameObject.activeInHierarchy) continue;
                if (!s.IsInteractable()) continue;

                // Skip world-space UI (e.g. in-game health bars)
                var canvas = s.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                candidates.Add(s);
            }

            // The game leaves the menu you came from switched on behind the one it
            // just opened, so every button in the scene is live at once. Reading
            // them as one list makes Settings sound like extra rows glued to the
            // main menu. Keep only the panel that is actually on top.
            Transform newScope = FindTopmostPanel(candidates);

            foreach (var s in candidates)
            {
                if (newScope == null || s.transform.IsChildOf(newScope))
                    _currentSelectables.Add(s);
            }

            // Sort top-to-bottom by screen Y position
            _currentSelectables.Sort((a, b) => GetScreenY(b).CompareTo(GetScreenY(a)));

            bool scopeChanged = newScope != _scopeRoot;
            _scopeRoot = newScope;

            // Log when selectables change
            if (_currentSelectables.Count != _lastSelectableCount)
            {
                Plugin.Logger.LogInfo($"Found {_currentSelectables.Count} UI selectables in {(newScope != null ? newScope.name : "scene")}.");
                _lastSelectableCount = _currentSelectables.Count;
            }

            if (_currentSelectables.Count == 0)
            {
                _currentIndex = -1;
                _lastSelected = null;
                return;
            }

            // A new panel is a new menu: name it and start at its first row rather
            // than leaving focus wherever the last menu left it.
            if (scopeChanged)
            {
                string menuName = DetectMenuName(newScope);
                if (!string.IsNullOrEmpty(menuName))
                    ScreenReaderManager.Speak(menuName);
                _lastSelected = null;
                SetIndex(0);
                return;
            }

            // Try to preserve current selection
            if (_lastSelected != null)
            {
                int idx = _currentSelectables.IndexOf(_lastSelected);
                if (idx >= 0)
                {
                    _currentIndex = idx;
                    return;
                }
            }

            // Sync to EventSystem's current selection
            var currentGO = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (currentGO != null)
            {
                var sel = currentGO.GetComponent<Selectable>();
                if (sel != null)
                {
                    int idx = _currentSelectables.IndexOf(sel);
                    if (idx >= 0)
                    {
                        _currentIndex = idx;
                        _lastSelected = sel;
                        return;
                    }
                }
            }

            // Nothing selected yet - select first item
            SetIndex(0);
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

        private void DetectExternalSelection()
        {
            if (EventSystem.current == null) return;
            var currentGO = EventSystem.current.currentSelectedGameObject;
            if (currentGO == null) return;

            var sel = currentGO.GetComponent<Selectable>();
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
            if (index < 0 || index >= _currentSelectables.Count) return;

            _currentIndex = index;
            var selectable = _currentSelectables[index];
            _lastSelected = selectable;

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

            announcement += $", {_currentIndex + 1} of {_currentSelectables.Count}";

            Plugin.Logger.LogInfo($"Announce: {announcement}");
            ScreenReaderManager.Speak(announcement);
        }

        private static string GetSelectableLabel(Selectable selectable)
        {
            var tmpText = selectable.GetComponentInChildren<TMPro.TMP_Text>(false);
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
                return tmpText.text.Trim();

            var uiText = selectable.GetComponentInChildren<Text>(false);
            if (uiText != null && !string.IsNullOrWhiteSpace(uiText.text))
                return uiText.text.Trim();

            var dropdown = selectable as Dropdown;
            if (dropdown != null && dropdown.captionText != null)
                return dropdown.captionText.text.Trim();

            var slider = selectable as Slider;
            if (slider != null)
            {
                string nearby = FindNearbyLabel(selectable);
                if (!string.IsNullOrEmpty(nearby))
                    return nearby;
            }

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

        /// <summary>
        /// The panel the player is really looking at: the container of whichever
        /// live button is drawn last. Unity draws later siblings, and higher
        /// sorting orders, on top - so the one that wins that comparison belongs
        /// to the panel that opened most recently.
        /// </summary>
        private static Transform FindTopmostPanel(List<Selectable> candidates)
        {
            if (candidates.Count == 0) return null;

            Selectable anchor = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                if (IsDrawnAbove(candidates[i], anchor))
                    anchor = candidates[i];
            }

            var canvas = anchor.GetComponentInParent<Canvas>();
            Transform canvasTransform = canvas != null ? canvas.transform : null;

            // Walk out of the button until we reach something that looks like a
            // panel - a backing image or a fade group - and holds more than the
            // one button, so a button sitting on its own artwork does not become
            // a menu of one.
            Transform node = anchor.transform.parent;
            Transform widest = null;
            while (node != null && node != canvasTransform)
            {
                widest = node;
                if (IsPanel(node) && CountWithin(candidates, node) > 1)
                    return node;
                node = node.parent;
            }

            return widest;
        }

        private static bool IsPanel(Transform t)
        {
            // A button's own background is not the panel that contains it.
            if (t.GetComponent<Selectable>() != null) return false;
            if (t.GetComponent<CanvasGroup>() != null) return true;
            if (t.GetComponent<Canvas>() != null) return true;

            var graphic = t.GetComponent<Graphic>();
            if (graphic != null && graphic.enabled && !(graphic is Text) && !(graphic is TMPro.TMP_Text))
                return true;

            return false;
        }

        private static int CountWithin(List<Selectable> candidates, Transform root)
        {
            int count = 0;
            foreach (var s in candidates)
            {
                if (s.transform.IsChildOf(root))
                    count++;
            }
            return count;
        }

        private static bool IsDrawnAbove(Selectable a, Selectable b)
        {
            int orderA = SortingOrderOf(a);
            int orderB = SortingOrderOf(b);
            if (orderA != orderB) return orderA > orderB;

            var pathA = HierarchyPath(a.transform);
            var pathB = HierarchyPath(b.transform);
            for (int i = 0; i < pathA.Count && i < pathB.Count; i++)
            {
                if (pathA[i] != pathB[i]) return pathA[i] > pathB[i];
            }
            return pathA.Count > pathB.Count;
        }

        private static int SortingOrderOf(Selectable s)
        {
            var canvas = s.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.sortingOrder : 0;
        }

        private static List<int> HierarchyPath(Transform t)
        {
            var path = new List<int>();
            for (Transform node = t; node != null; node = node.parent)
                path.Add(node.GetSiblingIndex());
            path.Reverse();
            return path;
        }

        private static string DetectMenuName(Transform scope)
        {
            if (scope != null)
            {
                string title = LargestHeading(scope);
                if (!string.IsNullOrEmpty(title))
                    return title;
                return CleanName(scope.gameObject.name);
            }

            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                if (canvas.renderMode == RenderMode.WorldSpace) continue;

                string title = LargestHeading(canvas.transform);
                if (!string.IsNullOrEmpty(title))
                    return title;
            }
            return null;
        }

        private static string LargestHeading(Transform root)
        {
            var texts = root.GetComponentsInChildren<TMPro.TMP_Text>(false);
            foreach (var t in texts)
            {
                if (t.GetComponentInParent<Selectable>() != null) continue;
                if (string.IsNullOrWhiteSpace(t.text)) continue;
                if (t.fontSize >= 24)
                    return t.text.Trim();
            }
            return null;
        }
    }
}
