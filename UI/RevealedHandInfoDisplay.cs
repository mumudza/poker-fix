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
    public class RevealedHandInfoDisplay : Benscript
    {
        [Header("Revealed Hand Info Display Parts (Must not be relatives)")]
        public Player ParentPlayer;
        public GameObject Display;
        public MeshRenderer[] CardFaces;
        public TMPro.TextMeshProUGUI BestHandDisplay;

        //[Header("Revealed Hand Info Display Data")]
        //public Vector2 CardFacePosition, CardFaceOffset;

        private string prevCard1Anim = string.Empty, prevCard2Anim = string.Empty;

        private bool ShouldDisplay => 
            ParentPlayer.Manager.GameStarted
            && ParentPlayer.Manager.CurStreet == GameManager.ShowdownStreet
            && ParentPlayer.HasOwner
            && !ParentPlayer.OwnedByLocal
            && ParentPlayer.Hand.FirstCard != -1
            && ParentPlayer.Manager._CheckedAtShowdown(ParentPlayer.PlayerNum);

        public override void Deserialize()
        {
            base.Deserialize();

            if (!ShouldDisplay)
            {
                Display.SetActive(false);
                return;
            }

            Card card1 = ParentPlayer.Manager.GameDeck.pool.Objects[ParentPlayer.Hand.FirstCard].GetComponent<Card>();
            Card card2 = ParentPlayer.Manager.GameDeck.pool.Objects[ParentPlayer.Hand.SecondCard].GetComponent<Card>();

            ChangeCard(0, card1.Rank, card1.Suit);
            ChangeCard(1, card2.Rank, card2.Suit);

            Material cardDesign = ParentPlayer.Manager.GameDeck.CardFrontMaterials[ParentPlayer.LocalSettingsPanel.Settings.DesignIndex];

            CardFaces[0].material = cardDesign;
            CardFaces[1].material = cardDesign;

            BestHandDisplay.text = ParentPlayer.HandNameDisplay;

            Display.SetActive(true);
        }
        public void ChangeCard(int card, int rank, int suit)
        {
            MeshRenderer cardFace = CardFaces[card];
            Animator animator = cardFace.GetComponent<Animator>();
            string curAnim = $"Card {suit * Card.Ranks + rank + 1}";

            //Vector2 scaledOffset = new Vector2(CardFaceOffset.x * rank, CardFaceOffset.y * suit);
            //Vector2 finalOffset = CardFacePosition + scaledOffset;

            //cardFace.transform.localPosition = new Vector3(finalOffset.x, finalOffset.y, cardFace.transform.localPosition.z);
            //cardFace.gameObject.GetComponent<Animator>().Play($"Card {suit * Card.Ranks + rank + 1}");

            if (card == 0)
            {
                if (prevCard1Anim != string.Empty)
                    animator.ResetTrigger(prevCard1Anim);
            }
            else
            {
                if (prevCard2Anim != string.Empty)
                    animator.ResetTrigger(prevCard2Anim);
            }

            animator.SetTrigger(curAnim);

            if (card == 0)
                prevCard1Anim = curAnim;
            else
                prevCard2Anim = curAnim;
        }

    }
}
