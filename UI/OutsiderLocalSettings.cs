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
    public class OutsiderLocalSettings : UdonSharpBehaviour
    {
        [Header("Outsider Local Settings Parts")]
        public LocalSettingsPanel LocalSettingsPanel;
        public GameObject HandRankingsCanvas;
        public GameObject MenuButtonsCanvas;

        public void _OpenLocalSettings()
        {
            LocalSettingsPanel.MatchSettings();
            LocalSettingsPanel.gameObject.SetActive(true);
            MenuButtonsCanvas.SetActive(false);
        }

        public void _CloseLocalSettings()
        {
            LocalSettingsPanel.gameObject.SetActive(false);
            MenuButtonsCanvas.SetActive(true);
        }

        public void _OpenHandRankings()
        {
            HandRankingsCanvas.SetActive(true);
            MenuButtonsCanvas.SetActive(false);
        }

        public void _CloseHandRankings()
        {
            HandRankingsCanvas.SetActive(false);
            MenuButtonsCanvas.SetActive(true);
        }

        public void _EnableOutsiderSettings()
        {
            LocalSettingsPanel.gameObject.SetActive(false);
            HandRankingsCanvas.SetActive(false);
            MenuButtonsCanvas.SetActive(true);
        }

        public void _DisableOutsiderSettings()
        {
            LocalSettingsPanel.gameObject.SetActive(false);
            HandRankingsCanvas.SetActive(false);
            MenuButtonsCanvas.SetActive(false);
        }
    }
}
