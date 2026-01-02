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
    public class CommunityCardInfoDisplay : Benscript
    {
        [Header("Community Card Info Display Parts (Must not be relatives)")]
        public GameManager Manager;
        public GameObject CardCanvas;
        public MeshRenderer[] CardFaces;
        public GameObject[] CardParents;

        //[Header("Community Card Info Display Data")]
        //public Vector2 CardFacePosition, CardFaceOffset;

        public float FadeScale = 12f;

        [Range(1f, 25f)]
        public float FadeDecay = 16f;

        private float[] opacityTargets = new float[GameManager.CommunityCardSize];
        private string[] prevAnims = { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };

        public override void Start()
        {
            base.Start();
            ResetOpacities();
        }

        public void Update()
        {
            if (!Manager.GameStarted)
                return;

            for (int i = 0; i < CardFaces.Length; ++i)
                //CardFaces[i].material.color = new Color(1f, 1f, 1f, DecayOpacity(i));
                CardParents[i].transform.localScale = DecayScale(i);
        }

        public override void Deserialize()
        {
            base.Deserialize();

            CardCanvas.SetActive(Manager.GameStarted && !Manager.Processing);

            if (!Manager.GameStarted)
            {
                //for (int i = 0; i < Card.Turn; ++i)
                //    opacityTargets[i] = 0f;

                //foreach (MeshRenderer cardFace in CardFaces)
                //    cardFace.material.color = new Color(1f, 1f, 1f, 0f);
                ResetOpacities();

                return;
            }

            if (Manager.CurStreet < GameManager.FlopStreet)
                return;

            Card[] communityCards = new Card[GameManager.CommunityCardSize];

            communityCards[Card.Flop1] = Manager.GameDeck.pool.Objects[Manager.Flop1CardIdx].GetComponent<Card>();
            communityCards[Card.Flop2] = Manager.GameDeck.pool.Objects[Manager.Flop2CardIdx].GetComponent<Card>();
            communityCards[Card.Flop3] = Manager.GameDeck.pool.Objects[Manager.Flop3CardIdx].GetComponent<Card>();
            communityCards[Card.Turn] = Manager.GameDeck.pool.Objects[Manager.TurnCardIdx].GetComponent<Card>();
            communityCards[Card.River] = Manager.GameDeck.pool.Objects[Manager.RiverCardIdx].GetComponent<Card>();

            for (int i = 0; i < GameManager.CommunityCardSize; ++i)
                ChangeCard(i, communityCards[i].Rank, communityCards[i].Suit);

            for (int i = 0; i < Card.Turn; ++i)
                //opacityTargets[i] = 1f;
                opacityTargets[i] = FadeScale;

            if (Manager.CurStreet < GameManager.TurnStreet)
                return;

            //opacityTargets[Card.Turn] = 1f;
            opacityTargets[Card.Turn] = FadeScale;

            if (Manager.CurStreet < GameManager.RiverStreet)
                return;

            //opacityTargets[Card.River] = 1f;
            opacityTargets[Card.River] = FadeScale;
        }

        public void ChangeCard(int card, int rank, int suit)
        {
            MeshRenderer cardFace = CardFaces[card];
            Animator animator = cardFace.GetComponent<Animator>();
            string prevAnim = prevAnims[card];
            string curAnim = $"Card {suit * Card.Ranks + rank + 1}";

            //Vector2 scaledOffset = new Vector2(CardFaceOffset.x * rank, CardFaceOffset.y * suit);
            //Vector2 finalOffset = CardFacePosition + scaledOffset;

            //cardFace.transform.localPosition = new Vector3(finalOffset.x, finalOffset.y, cardFace.transform.localPosition.z);

            if (prevAnim != string.Empty)
                animator.ResetTrigger(prevAnim);

            animator.SetTrigger(curAnim);

            prevAnims[card] = curAnim;
        }

        public void ResetOpacities()
        {
            for (int i = 0; i < CardFaces.Length; ++i)
            {
                opacityTargets[i] = 0f;
                //CardFaces[i].material.color = new Color(1f, 1f, 1f, 0f);
                CardParents[i].transform.localScale = new Vector3(0f, 0f, 1f);
            }
        }

        public void ChangeDesign(uint design)
        {
            foreach (var card in CardFaces)
                card.material = Manager.GameDeck.CardFrontMaterials[design];
        }

        //private float DecayOpacity(int idx) => Mathf.Lerp(opacityTargets[idx], CardFaces[idx].material.color.a, Mathf.Exp(-FadeDecay * Time.deltaTime));
        private Vector3 DecayScale(int idx)
        {
            float interpolant = Mathf.Exp(-FadeDecay * Time.deltaTime);
            float target = opacityTargets[idx];
            Vector3 current = CardParents[idx].transform.localScale;

            return Vector3.Lerp(new Vector3(target, target, 1f), current, interpolant);
        }
    }
}
