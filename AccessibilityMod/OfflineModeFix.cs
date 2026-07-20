using HarmonyLib;
using InfimaGames.LowPolyShooterPack;
using Photon.Pun;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Fixes the broken offline mode flow in MatchmakingManager.
    ///
    /// The vanilla code has two bugs:
    /// 1. OnConnectedToMasterOffline() sets offlineRoomRequested = false before
    ///    the disconnect callback fires, so JoinOfflineRoom() never creates a room.
    /// 2. Setting PhotonNetwork.OfflineMode = true triggers OnConnectedToMaster()
    ///    synchronously, which calls JoinLobby() and interferes with the room join.
    ///
    /// This patch replaces SetOffline() and JoinRoomOffline() with a clean flow:
    ///   Disconnect (if needed) -> set OfflineMode -> create room with bots.
    /// It also guards OnConnectedToMaster() so it skips JoinLobby() during the
    /// offline transition.
    /// </summary>
    [HarmonyPatch]
    public static class OfflineModeFix
    {
        private static bool _goingOffline;

        /// <summary>
        /// Replace SetOffline entirely. The original calls OnConnectedToMasterOffline
        /// which has the flag-clearing bug. We just mark the intent and disconnect.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchmakingManager), nameof(MatchmakingManager.SetOffline))]
        static bool SetOffline_Prefix(MatchmakingManager __instance)
        {
            Plugin.Logger.LogInfo("OfflineModeFix: SetOffline intercepted");
            _goingOffline = true;

            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            else
            {
                EnterOfflineAndJoin(__instance);
            }

            return false; // skip original
        }

        /// <summary>
        /// Replace JoinRoomOffline entirely. Same clean flow.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchmakingManager), nameof(MatchmakingManager.JoinRoomOffline))]
        static bool JoinRoomOffline_Prefix(MatchmakingManager __instance)
        {
            Plugin.Logger.LogInfo("OfflineModeFix: JoinRoomOffline intercepted");
            _goingOffline = true;

            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            else
            {
                EnterOfflineAndJoin(__instance);
            }

            return false; // skip original
        }

        /// <summary>
        /// Patch OnDisconnected so that when we're going offline, we enter
        /// offline mode and join a room instead of showing the disconnect panel.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchmakingManager), "OnDisconnected")]
        static bool OnDisconnected_Prefix(MatchmakingManager __instance,
            Photon.Realtime.DisconnectCause cause)
        {
            if (!_goingOffline)
                return true; // let original run

            Plugin.Logger.LogInfo($"OfflineModeFix: OnDisconnected during offline transition (cause={cause}), entering offline mode");
            EnterOfflineAndJoin(__instance);
            return false; // skip original (prevents ShowDisconnectedPanel)
        }

        /// <summary>
        /// Guard OnConnectedToMaster: when transitioning to offline mode,
        /// skip the original which calls JoinLobby() and other online-only logic.
        /// Photon fires this callback synchronously when OfflineMode is set to true.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchmakingManager), "OnConnectedToMaster")]
        static bool OnConnectedToMaster_Prefix()
        {
            if (_goingOffline)
            {
                Plugin.Logger.LogInfo("OfflineModeFix: Suppressing OnConnectedToMaster during offline transition");
                return false; // skip original
            }
            return true;
        }

        private static void EnterOfflineAndJoin(MatchmakingManager instance)
        {
            _goingOffline = false;

            if (!PhotonNetwork.OfflineMode)
            {
                Plugin.Logger.LogInfo("OfflineModeFix: Setting PhotonNetwork.OfflineMode = true");
                PhotonNetwork.OfflineMode = true;
            }

            // Now create a room directly. In offline mode, CreateRoom succeeds
            // immediately and triggers OnJoinedRoom synchronously.
            Plugin.Logger.LogInfo("OfflineModeFix: Creating offline room");

            // Set bots_only so the match fills with bots
            AccessTools.Field(typeof(MatchmakingManager), "bots_only").SetValue(instance, true);

            instance.JoinRandomRoom(true);
        }
    }
}
