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
    public class Translatable : UdonSharpBehaviour
    {
        public const uint LangEN = 0, LangFR = 1, LangES = 2, LangDE = 3, LangJP = 4, LangKR = 5, LangFI = 6, NumLangs = 7;
        public const string FullWidthAccommodation = "<line-height=100%>", FullWidthLessSquishedAccommodation = "<line-height=110%>";

        [Header("Supported Languages in Order: EN, FR, ES, DE, JP, KR, FI")]

        public GameManager Manager;
        public Player ParentPlayer;
        public LocalSettingsPanel LocalSettingsPanel;

        public LocalSettings Settings
        {
            get
            {
                if (ParentPlayer != null)
                    return ParentPlayer.Manager.LocalSettings;
                else if (LocalSettingsPanel != null)
                    return LocalSettingsPanel.Settings;
                return Manager.LocalSettings;
            }
        }

        public virtual void Start()
        {
            if (ParentPlayer != null)
                ParentPlayer.Manager._AddTranslatable(this);
            else if (LocalSettingsPanel != null)
                LocalSettingsPanel.Settings.Manager._AddTranslatable(this);
            else
                Manager._AddTranslatable(this);

            Translate();
        }

        public virtual void Translate() { }
    }
}
