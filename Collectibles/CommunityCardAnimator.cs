// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class CommunityCardAnimator : SyncedAnimator
    {
        [Header("Community Card Animator Parts (Must not be relatives)")]
        public GameManager Manager;
        public Transform ReferencePoint;
        public LayeredEffectPlayer Effect;

        [Header("Community Card Animator Data")]
        [Range(Card.Flop1, Card.River)]
        public int CommunityCardIdx = 0;

        private Card cardToAnim = null;

        public void GetCard()
        {
            if (!OwnedByLocal || !Manager.GameStarted)
                return;

            cardToAnim = null;

            if (CommunityCardIdx == Card.Flop2)
                cardToAnim = Manager.GameDeck.GetCard(Manager.Flop2CardIdx).GetComponent<Card>();
            else if (CommunityCardIdx == Card.Flop3)
                cardToAnim = Manager.GameDeck.GetCard(Manager.Flop3CardIdx).GetComponent<Card>();
            else if (CommunityCardIdx == Card.Turn)
                cardToAnim = Manager.GameDeck.GetCard(Manager.TurnCardIdx).GetComponent<Card>();
            else if (CommunityCardIdx == Card.River)
                cardToAnim = Manager.GameDeck.GetCard(Manager.RiverCardIdx).GetComponent<Card>();
            else
                cardToAnim = Manager.GameDeck.GetCard(Manager.Flop1CardIdx).GetComponent<Card>();
        }

        public void _StartSparkle() => Effect.Play();
        public void _StopSparkle() => Effect.Stop();
    }
}
