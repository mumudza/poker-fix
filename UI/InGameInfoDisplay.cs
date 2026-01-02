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
    public class InGameInfoDisplay : UdonSharpBehaviour
    {
        public const string DisabledInfoColorTag = "<color=#FFFFFF44>", ColorClose = "</color>";
        public const string ZeroLineHeightTag = "<line-height=0%>", LineHeightClose = "</line-height>";
        public const string RightAlignTag = "<align=\"right\">", AlignClose = "</align>";
        public const string BoldTag = "<b>", BoldClose = "</b>";
        public const string Newline = "\n";

        private readonly string[] CurrentBetLabels =
        {
              "Current Bet"
            , "Pari actuel"
            , "Apuesta actual"
            , "Aktuelle Wette"
            , "現在のベット"
            , "현재의 베트"
            , "Tämänhetkinen Panos"
        };

        private readonly string[] MainPotLabels =
        {
              "Main Pot"
            , "Pot principal"
            , "Bote principal"
            , "Hauptpot"
            , "メインポット"
            , "메인 팟"
            , "Pääpotti"
        };

        [Header("In Game Info Display Parts")]

        public GameManager Manager;
        public LocalSettings Settings;

        public TMPro.TextMeshProUGUI MonetaryInfoFirstHalf, MonetaryInfoSecondHalf;
        public TMPro.TextMeshProUGUI TurnPlayerName;
        public TMPro.TextMeshProUGUI RoundOverTitle;
        public TMPro.TextMeshProUGUI ProcessingMessage;
        public UnityEngine.UI.Image ProcessingThrobber;

        [Header("In Game Info Display Data")]
        public float ThrobberSpeed = 1f;

        public void Update()
        {
            if (!ProcessingThrobber.gameObject.activeSelf)
                return;

            ProcessingThrobber.transform.localRotation *= Quaternion.AngleAxis(ThrobberSpeed * Time.deltaTime, Vector3.back);
        }

        public void SetInfo()
        {
            bool midround = !Manager.Processing && !Manager.RoundEnded;
            bool roundEnded = !Manager.Processing && Manager.RoundEnded;

            TurnPlayerName.gameObject.SetActive(midround);
            RoundOverTitle.gameObject.SetActive(roundEnded);

            ProcessingMessage.gameObject.SetActive(Manager.Processing);
            ProcessingThrobber.gameObject.SetActive(Manager.Processing);

            MonetaryInfoFirstHalf.gameObject.SetActive(!Manager.Processing);
            MonetaryInfoSecondHalf.gameObject.SetActive(!Manager.Processing);

            if (midround)
            {
                int numPots = Manager.NumPots;
                int totalPots = Manager.Players.Length - 1;
                int halfTotalPots = totalPots / 2;

                MonetaryInfoFirstHalf.text = BuildInfoLine(CurrentBetLabels[Settings.LanguageIndex], Manager.CurGreatestBet, Manager.CurStreet != GameManager.ShowdownStreet);
                MonetaryInfoSecondHalf.text = "";

                for (int i = 0; i < Manager.Players.Length - 1; ++i)
                {
                    TMPro.TextMeshProUGUI info = i >= halfTotalPots ? MonetaryInfoSecondHalf : MonetaryInfoFirstHalf;
                    int potAmt = Manager.GetPot(i);

                    if (i == 0)
                        info.text += BuildInfoLine(MainPotLabels[Settings.LanguageIndex], potAmt, true);

                    // REMARK: Will break if there are >1 pots with $0 (shouldn't be possible)
                    else
                        info.text += BuildInfoLine(BuildSidePotString(i), potAmt, i < numPots && potAmt > 0);
                }

                TurnPlayerName.text = BuildTurnString();
            }
        }

        private string BuildInfoLine(string label, int amount, bool enabled)
        {
            string result = "";

            if (!enabled)
                result += DisabledInfoColorTag;

            result += ZeroLineHeightTag + label + Newline + RightAlignTag + BoldTag + Settings.FormatCurrency(enabled ? amount : 0) + BoldClose + AlignClose + LineHeightClose + (Manager.LocalSettings.LanguageIndex == Translatable.LangJP || Manager.LocalSettings.LanguageIndex == Translatable.LangKR ? Translatable.FullWidthAccommodation : "") + Newline;

            if (!enabled)
                result += ColorClose;

            return result;
        }

        private string BuildTurnString()
        {
            string playerName = Manager.Players[Manager.CurPlayerIndex].Owner.displayName;

            switch (Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    return $"A ton tour, {playerName} !";

                case Translatable.LangES:
                    return $"¡Tu turno, {playerName}!";

                case Translatable.LangDE:
                    return $"Sie sind dran, {playerName}!";

                case Translatable.LangJP:
                    return $"あなたの番よ、{playerName}！";

                case Translatable.LangKR:
                    return $"당신의 차례입니다, {playerName}!";

                case Translatable.LangFI:
                    return $"Sinun vuorosi, {playerName}!";

                default:
                    return $"Your turn, {playerName}!";
            }
        }

        private string BuildSidePotString(int idx)
        {
            switch (Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    return $"Pot parallèle n°{idx}";

                case Translatable.LangES:
                    return $"{idx}<sup>o</sup> bote lateral";

                case Translatable.LangDE:
                    return $"{idx}. Side Pot";

                case Translatable.LangJP:
                    return $"サイドポット{idx}号";

                case Translatable.LangKR:
                    return $"{idx}번 사이드 팟";

                case Translatable.LangFI:
                    return $"Sivupotti #{idx}";

                default:
                    return $"Side Pot #{idx}";
            }
        }
    }
}
