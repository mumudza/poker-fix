// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    public class ChipDisplay : UdonSharpBehaviour
    {
        public GameManager Manager;
        public GameObject[] PileModels;
        public ChipConfiguration[] ChipConfigurations;
        public float[] PilePercentages;

        public void DisplayAmount(int chips)
        {
            foreach (GameObject pileModel in PileModels)
                pileModel.SetActive(false);

            if (chips == 0)
                return;

            int configIdx = PilePercentages.Length - 1;
            float pilePercentage = chips / (float)Manager.TotalStartingChips;

            for (int i = PilePercentages.Length - 1; i > 0; --i)
            {
                if (PilePercentages[i] < pilePercentage)
                    break;

                configIdx = i;
            }

            ChipConfigurations[configIdx].EnableModels();
        }
    }
}
