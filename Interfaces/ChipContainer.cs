// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    public class ChipContainer : Benscript
    {
        [Header("Chip Container Parts (Must be relatives)")]

        public ChipDisplay PileVisual;

        [Header("Chip Container Parts (Must not be relatives)")]

        public TMPro.TextMeshProUGUI OptionalAmountText;
        public LocalSettings Settings;

        [Header("Chip Container Data")]
        public bool VisibleToOwnerOnly = false;

        [UdonSynced]
        private int chips = 0;

        public string DisplayString => GetPrefix() + Settings.FormatCurrency(GetChips());

        public int GetChips() => chips;
        public void SetChips(int c)
        {
            chips = c;
            Serialize();
        }
        public void AddChips(int c) => SetChips(chips + c);
        public void RemoveChips(int c) => SetChips(c >= chips ? 0 : chips - c);

        public override void Deserialize()
        {
            base.Deserialize();

            PileVisual.DisplayAmount(chips);

            if (OptionalAmountText)
            {
                OptionalAmountText.gameObject.SetActive(!VisibleToOwnerOnly || OwnedByLocal);

                if (!OptionalAmountText.gameObject.activeSelf)
                    return;

                OptionalAmountText.text = DisplayString;
            }
        }

        public virtual string GetIndicator() => string.Empty;

        private string GetPrefix() => GetIndicator() == string.Empty ? string.Empty : GetIndicator() + " ";
    }
}
