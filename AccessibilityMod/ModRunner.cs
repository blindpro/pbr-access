using System;
using UnityEngine;

namespace AccessibilityMod
{
    public class ModRunner : MonoBehaviour
    {
        private MenuNavigator _menuNavigator;
        private HudReader _hudReader;
        private AccessibleInputController _inputController;
        private NavigationAssistant _navigationAssistant;
        private AimAssist _aimAssist;
        private bool _hasSpokenReady;

        private void Awake()
        {
            _menuNavigator = new MenuNavigator();
            _hudReader = new HudReader();
            _inputController = new AccessibleInputController();
            _navigationAssistant = new NavigationAssistant();
            _aimAssist = new AimAssist();
        }

        private void Update()
        {
            try
            {
                if (!_hasSpokenReady)
                {
                    _hasSpokenReady = true;
                    ScreenReaderManager.Speak("Accessibility mod ready");
                }

                _menuNavigator.Tick();
                _hudReader.Tick();
                _inputController.Tick();
                _navigationAssistant.Tick();
                _aimAssist.Tick();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ModRunner error: {ex}");
            }
        }

        private void OnDestroy()
        {
            ScreenReaderManager.Shutdown();
        }
    }
}
