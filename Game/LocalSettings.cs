// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using System.Globalization;
using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalSettings : UdonSharpBehaviour
    {
        public const string CompatibleYenSign = "\u00A5";
        public const uint SliderInput = 0, NumpadInput = 1;
        private readonly string[] Cultures = { "en-US", "en-GB", "de-DE", "ja-JP", "ko-KR" };

        [Header("Local Settings Parts")]

        public GameManager Manager;

        private uint designIdx = 0;
        private uint cultureIdx = 0;
        private uint inputIdx = 0;
        private uint langIdx = Translatable.LangEN;

        [HideInInspector]
        public bool ActionHintsEnabled = true, HandNameEnabled = true;

        [HideInInspector]
        public float MusicVolume = 0.5f;

        [HideInInspector]
        public float SFXVolume = 0.5f;

        public uint DesignIndex => designIdx;
        public uint CultureIndex => cultureIdx;
        public uint InputIndex => inputIdx;
        public uint LanguageIndex => langIdx;
        public string Culture => Cultures[cultureIdx];

        public string FormatCurrency(int money)
        {
            string moneyStr = money.ToString("C0", CultureInfo.GetCultureInfo(Cultures[cultureIdx]));

            // REMARK: Kludge here is necessary, ja-JP uses \uFFE5 which Arcane Nine can't render
            // The font uses the ASCII yen as opposed to the Unicode yen
            if (Culture == "ja-JP")
                moneyStr = CompatibleYenSign + moneyStr.Substring(1);

            return moneyStr;
        }

        public void NextCardDesign()
        {
            designIdx++;

            if (designIdx >= Manager.GameDeck.CardFrontMaterials.Length)
                designIdx = 0;

            Manager.GameDeck.ChangeDesigns(designIdx);
        }

        public void PrevCardDesign()
        {
            if (designIdx == 0)
                designIdx = (uint)Manager.GameDeck.CardFrontMaterials.Length;

            designIdx--;

            Manager.GameDeck.ChangeDesigns(designIdx);
        }

        public void SetCurrencyCulture(uint idx)
        {
            cultureIdx = idx;
            Manager._DeserializeLocally();
        }

        public void SetInputMethod(uint idx)
        {
            inputIdx = idx;
            Manager._DeserializeLocally();
        }

        public void SetActionHints(bool on)
        {
            ActionHintsEnabled = on;
            Manager._DeserializeLocally();
        }

        public void SetHandName(bool on)
        {
            HandNameEnabled = on;
            Manager._DeserializeLocally();
        }

        public void SetLanguage(uint idx)
        {
            langIdx = idx;
            Manager._DeserializeLocally();
        }
    }
}
