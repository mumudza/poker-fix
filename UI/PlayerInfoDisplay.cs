// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerInfoDisplay : Benscript
    {
        public const string PlayerSprite = "<sprite=6>", GameMasterSprite = "<sprite=8>";

        [Header("Player Info Display Parts (Must not be relatives)")]
        public Player ParentPlayer;
        public TMPro.TextMeshProUGUI PlayerNameText;
        public TMPro.TextMeshProUGUI OverheadText;
        public GameObject TurnIndicator;

        [Header("Player Info Display Data")]
        public float HeadOffset = 0f;

        private bool ShouldDisplay => ParentPlayer.HasOwner && !ParentPlayer.OwnedByLocal;
        private bool PlayersTurn => ParentPlayer.Manager.GameStarted && !ParentPlayer.Manager.Processing && !ParentPlayer.Manager.RoundEnded && ParentPlayer.Manager.CurPlayerIndex == ParentPlayer.PlayerNum;

        private string PlayerNameDisplay =>
            PlayerSprite
            + (ParentPlayer.Manager.OwnerID == ParentPlayer.OwnerID ? GameMasterSprite : "")
            + (ParentPlayer.Manager.GameStarted && ParentPlayer.IsDealer ? Player.DealerButton : "")
            + (ParentPlayer.Manager.GameStarted && !ParentPlayer.Manager.Processing && !ParentPlayer.Manager.RoundEnded && !ParentPlayer.LateJoiner && !ParentPlayer.Manager.PlayerFolded(ParentPlayer.PlayerNum) && ParentPlayer.Bankroll.IsChipLeader() ? Bankroll.ChipLeaderEmoji : "")
            + " " + Owner.displayName;

        public void Update()
        {
            if (Utilities.IsValid(ParentPlayer.Owner) && ParentPlayer.HasOwner)
            {
                Vector3 headPos = ParentPlayer.Owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                headPos.y += HeadOffset;
                transform.position = headPos;
            }
        }

        public override void Deserialize()
        {
            base.Deserialize();

            PlayerNameText.gameObject.SetActive(ParentPlayer.HasOwner);

            if (ParentPlayer.HasOwner)
                PlayerNameText.text = PlayerNameDisplay;

            OverheadText.gameObject.SetActive(ShouldDisplay);
            TurnIndicator.SetActive(ShouldDisplay && PlayersTurn);

            if (!ShouldDisplay)
                return;

            if (ParentPlayer.Manager.GameStarted)
                DisplayInGameInfo();
            else
                DisplayUnstartedInfo();
        }

        private void DisplayUnstartedInfo()
        {
            OverheadText.text = string.Empty;
        }

        private void DisplayInGameInfo()
        {
            OverheadText.text = string.Empty;

            if (ParentPlayer.IsDealer)
                OverheadText.text += Player.DealerButton + " " + BuildDealerText() + "\n";

            if (!ParentPlayer.Manager.Processing && !ParentPlayer.Manager.RoundEnded && !ParentPlayer.LateJoiner && !ParentPlayer.Manager.PlayerFolded(ParentPlayer.PlayerNum) && ParentPlayer.Bankroll.IsChipLeader())
                OverheadText.text += Bankroll.ChipLeaderEmoji + " " + BuildChipLeaderText() + "\n";

            OverheadText.text += ParentPlayer.Bankroll.DisplayString + "\n";
            OverheadText.text += ParentPlayer.BetPile.DisplayString;
        }

        private string BuildDealerText()
        {
            switch (ParentPlayer.LocalSettingsPanel.Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    return "Donneur";

                case Translatable.LangES:
                    return "Repartidor";

                case Translatable.LangDE:
                    return "Geber";

                case Translatable.LangJP:
                    return "ディーラー";

                case Translatable.LangKR:
                    return "딜러";

                case Translatable.LangFI:
                    return "Jakaja";

                default:
                    return "Dealer";
            }
        }

        private string BuildChipLeaderText()
        {
            switch (ParentPlayer.LocalSettingsPanel.Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    return "Meneur·se en jetons";

                case Translatable.LangES:
                    return "Chipleader";

                case Translatable.LangDE:
                    return "Chipleader";

                case Translatable.LangJP:
                    return "チップリーダー";

                case Translatable.LangKR:
                    return "칩 리더";

                case Translatable.LangFI:
                    return "Pelimerkkien johtaja";

                default:
                    return "Chipleader";
            }
        }
    }
}
