using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

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
        private const float RescanInterval = 3.0f;
        private int _lastSelectableCount;
        private bool _wasPausedInMenu;

        /// <summary>
        /// Initializes a new instance of the MenuNavigator class.
        /// Hooks into the scene loaded event to force an immediate rescan.
        /// </summary>
        public MenuNavigator()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                _nextRescanTime = 0f;
            };
        }

        public void Tick()
        {
            // Track game pause/resume transitions
            var player = CharacterMultiplayer.GetMainPlayer();
            bool isDead = player != null && player.IsDead();
            bool currentPaused = !isDead && (
                                 (HP.Generics.PauseManager.instance != null && HP.Generics.PauseManager.instance.Bool_IsGamePaused) 
                                 || (MatchmakingManager.Instance != null 
                                     && MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing 
                                     && Cursor.visible 
                                     && !LootMenu.IsOpen 
                                     && !InventoryMenu.IsOpen)
                                 );

            if (currentPaused != _wasPausedInMenu)
            {
                _wasPausedInMenu = currentPaused;
                if (currentPaused)
                {
                    ScreenReaderManager.Speak(Loc.Get("game_paused"));
                    _nextRescanTime = 0f; // Force immediate rebuild of pause menu
                }
                else
                {
                    ScreenReaderManager.Speak(Loc.Get("game_resumed"));
                }
            }

            // The loot list and the inventory borrow Up, Down and Enter while they
            // are open; HUD buttons must not answer them at the same time.
            if (LootMenu.IsOpen || InventoryMenu.IsOpen) return;

            // Completely skip menu checks during active unpaused gameplay to avoid FindObjectsOfType CPU overhead.
            if (!IsInMenu())
            {
                if (_currentSelectables.Count > 0)
                {
                    _currentSelectables.Clear();
                    _currentIndex = -1;
                    _lastSelected = null;
                    _lastSelectableCount = 0;
                }
                return;
            }

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

            if (EventSystem.current == null)
            {
                _currentIndex = -1;
                _lastSelected = null;
                return;
            }

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

                _currentSelectables.Add(s);
            }

            // Sort top-to-bottom by screen Y position
            _currentSelectables.Sort((a, b) => GetScreenY(b).CompareTo(GetScreenY(a)));

            // Log when selectables change
            if (_currentSelectables.Count != _lastSelectableCount)
            {
                Plugin.Logger.LogInfo($"Found {_currentSelectables.Count} UI selectables.");
                if (_currentSelectables.Count > 0 && _lastSelectableCount == 0)
                {
                    string menuName = DetectMenuName();
                    if (!string.IsNullOrEmpty(menuName))
                        ScreenReaderManager.Speak(menuName);
                }
                _lastSelectableCount = _currentSelectables.Count;
            }

            if (_currentSelectables.Count == 0)
            {
                _currentIndex = -1;
                _lastSelected = null;
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
            if (idx < 0)
            {
                // UI changed externally. Rebuild immediately to discover new selectables!
                RebuildSelectablesList();
                idx = _currentSelectables.IndexOf(sel);
            }

            // Always update _lastSelected to the currently selected Component,
            // even if it was filtered out of our navigation list.
            // This prevents calling RebuildSelectablesList() on every frame!
            _lastSelected = sel;

            if (idx >= 0)
            {
                _currentIndex = idx;
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

        private void ActivateSelectable(Selectable selectable)
        {
            if (selectable is Button button)
            {
                button.onClick.Invoke();
                ScreenReaderManager.Speak("activated");
                _nextRescanTime = 0f;
                return;
            }

            if (selectable is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                string state = toggle.isOn ? "checked" : "not checked";
                ScreenReaderManager.Speak(state);
                _nextRescanTime = 0f;
                return;
            }

            if (selectable is Dropdown dropdown)
            {
                dropdown.Show();
                ScreenReaderManager.Speak("opened dropdown");
                _nextRescanTime = 0f;
                return;
            }

            if (selectable is InputField inputField)
            {
                inputField.ActivateInputField();
                ScreenReaderManager.Speak("editing");
                _nextRescanTime = 0f;
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

        private static string DetectMenuName()
        {
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                if (canvas.renderMode == RenderMode.WorldSpace) continue;

                var texts = canvas.GetComponentsInChildren<TMPro.TMP_Text>(false);
                foreach (var t in texts)
                {
                    if (t.GetComponentInParent<Selectable>() != null) continue;
                    if (string.IsNullOrWhiteSpace(t.text)) continue;
                    if (t.fontSize >= 24)
                        return t.text.Trim();
                }
            }
            return null;
        }

        private static bool IsInMenu()
        {
            if (EventSystem.current == null) return false;

            if (MatchmakingManager.Instance == null) return true;
            var status = MatchmakingManager.Instance.GetRoomStatus();
            if (status != MatchmakingManager.RoomStatus.Playing) return true;

            var main = CharacterMultiplayer.GetMainPlayer();
            if (main != null && main.IsDead()) return true;

            if (HP.Generics.PauseManager.instance != null && HP.Generics.PauseManager.instance.Bool_IsGamePaused) return true;

            if (Cursor.visible) return true;

            return false;
        }
    }
}
