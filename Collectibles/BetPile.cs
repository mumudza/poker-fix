// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BetPile : ChipContainer
    {
        public const string BettedEmoji = "<sprite=3>";

        [Header("Bet Pile Parts (Must not be relatives)")]

        public Player ParentPlayer;

        public void ResetBet()
        {
            SetChips(0);
        }

        public override string GetIndicator() => BettedEmoji;

        public override void Deserialize()
        {
            base.Deserialize();
            ParentPlayer.PlayerInfoDisplay.Deserialize();
        }
    }
}
