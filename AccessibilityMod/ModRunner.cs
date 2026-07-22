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
        private AudioTargeting _audioTargeting;
        private LootMenu _lootMenu;
        private InventoryMenu _inventoryMenu;
        private HealSlot _healSlot;
        private SafeZoneNav _safeZoneNav;
        private MapGrid _mapGrid;
        private LockDiagnostics _lockDiagnostics;
        private bool _hasSpokenReady;

        private void Awake()
        {
            _menuNavigator = new MenuNavigator();
            _hudReader = new HudReader();
            _inputController = new AccessibleInputController();
            _navigationAssistant = new NavigationAssistant();
            _aimAssist = new AimAssist();
            _audioTargeting = new AudioTargeting();
            _lootMenu = new LootMenu();
            _inventoryMenu = new InventoryMenu();
            _healSlot = new HealSlot();
            _safeZoneNav = new SafeZoneNav();
            _mapGrid = new MapGrid();
            _lockDiagnostics = new LockDiagnostics();
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

                // The loot list runs first: it claims E, Up, Down and Enter while
                // it is open, before anything else can act on them.
                _lootMenu.Tick();
                _inventoryMenu.Tick();
                _menuNavigator.Tick();
                _hudReader.Tick();
                // Before the input controller: the heal slot decides whether Left
                // Control fires this frame or drinks.
                _healSlot.Tick();
                _inputController.Tick();
                _navigationAssistant.Tick();
                _aimAssist.Tick();
                _audioTargeting.Tick();
                _safeZoneNav.Tick();
                _mapGrid.Tick();
                _lockDiagnostics.Tick();
                HitAnnouncer.Tick();
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
