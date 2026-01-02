// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class THMMultiManager : UdonSharpBehaviour
    {
        [Tooltip("The list of tables to manage. (Be sure to set the multimanager properties of each table.)")]
        public GameManager[] Managers;

        [Header("Multimanager Settings")]

        [Tooltip("Whether to turn the tables on or off (for performance reasons).")]
        public bool TablesEnabled = true;

        public void Start() => SendCustomEventDelayedFrames(nameof(_SetFirstEnable), 1);

        public void _SetFirstEnable() => _SetTablesEnabled(TablesEnabled);

        public bool PlayerHasJoined(int playerID)
        {
            if (Managers == null || Managers.Length <= 0)
                return false;

            foreach (GameManager manager in Managers)
            {
                if (manager.PlayerHasJoined(playerID, true))
                    return true;
            }

            return false;
        }

        public void DeserializeOthers(GameManager requester)
        {
            foreach (GameManager manager in Managers)
            {
                if (manager == requester)
                    continue;

                manager._DeserializeLocally();
            }
        }

        public void _SetTablesEnabled(bool toggle)
        {
            TablesEnabled = toggle;

            foreach (GameManager manager in Managers)
                manager.gameObject.SetActive(toggle);
        }

        public void _ToggleTables() => _SetTablesEnabled(!TablesEnabled);
    }
}
