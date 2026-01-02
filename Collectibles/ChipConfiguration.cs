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
    public class ChipConfiguration : UdonSharpBehaviour
    {
        public GameObject[] ChipModels;

        public void EnableModels()
        {
            foreach (GameObject pileModel in ChipModels)
                pileModel.SetActive(true);
        }
    }
}
