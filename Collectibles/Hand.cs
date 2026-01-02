// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace ThisIsBennyK.TexasHoldEm
{
    [RequireComponent(typeof(VRCObjectSync))]
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Hand : Benscript
    {
        public const int Size = 2, FinalSize = 5;

        public const float MinimumPutdownDistance = 0.05f;

        public const int
            HighCard = 0,
            OnePair = 1,
            TwoPair = 2,
            ThreeOfAKind = 3,
            Straight = 4,
            Flush = 5,
            FullHouse = 6,
            FourOfAKind = 7,
            StraightFlush = 8,
            RoyalFlush = 9,
            NumPokerHands = 10;

        [Header("Hand Parts (Must not be relatives)")]

        public Player ParentPlayer;
        public VRCPickup Pickup;
        public VRCObjectSync ObjectSync;

        public Transform Rotator;
        public Transform Card1Syncer, Card2Syncer;

        public SFXPlayer PickupSFX, PutdownSFX;

        [UdonSynced]
        private int cardIdx1 = -1;
        [UdonSynced]
        private int cardIdx2 = -1;

        private Card card1Comp = null, card2Comp = null;

        public int FirstCard => cardIdx1;
        public int SecondCard => cardIdx2;

        public Card FirstCardComponent => OwnedByLocal ? card1Comp : null;
        public Card SecondCardComponent => OwnedByLocal ? card2Comp : null;

        [HideInInspector]
        public bool InArea = false;

        private Vector3 lastPosition = new Vector3();

        private bool justPickedUp = false;

        public override void Start()
        {
            lastPosition = transform.position;
            base.Start();
        }

        public void Update()
        {
            // Try to get drawn cards as fast as possible when the manager is dealing them
            // REMARK: This is a terrible solution -- but we can't afford too many network calls to make this more efficient
            // and we barely have anything running in update loops anyway
            if (OwnedByLocal && ParentPlayer.Manager.GameStarted)
            {
                if (cardIdx1 == -1 || cardIdx2 == -1 || cardIdx1 == cardIdx2)
                    ReceiveCards();
            }

            if (Pickup.IsHeld)
                lastPosition = transform.position;
        }

        public void ReceiveCards()
        {
            if (ParentPlayer.LateJoiner)
            {
                ResetCards();
                return;
            }

            int[] cardIdxs = ParentPlayer.Manager.GameDeck.GetCardIndices();

            if (cardIdxs != null)
            {
                // If we're the player holding the community cards, account for that
                if (cardIdxs.Length >= Size)
                {
                    foreach (int idx in cardIdxs)
                    {
                        if (ParentPlayer.Manager.IsCommunityCard(idx))
                            continue;
                        else if (cardIdx1 == -1 && cardIdx2 != idx)
                            cardIdx1 = idx;
                        else if (cardIdx2 == -1 && cardIdx1 != idx)
                            cardIdx2 = idx;
                        else
                            break;
                    }
                }
                else
                {
                    cardIdx1 = cardIdxs[0];
                    cardIdx2 = cardIdxs[1];
                }

                if (cardIdx1 != -1)
                {
                    GameObject card1Obj = ParentPlayer.Manager.GameDeck.GetCard(cardIdx1);

                    if (card1Obj != null)
                    {
                        card1Comp = card1Obj.GetComponent<Card>();
                        card1Comp.OwnByLocal();
                    }
                }
                if (cardIdx2 != -1)
                {
                    GameObject card2Obj = ParentPlayer.Manager.GameDeck.GetCard(cardIdx2);

                    if (card2Obj != null)
                    {
                        card2Comp = card2Obj.GetComponent<Card>();
                        card2Comp.OwnByLocal();
                    }
                }
            }
            else
                ResetCards();
        }

        public void ResetCards()
        {
            cardIdx1 = -1;
            cardIdx2 = -1;

            card1Comp = null;
            card2Comp = null;
        }

        public void Respawn()
        {
            InArea = false;

            // If we rotated the hand, reset it
            Rotator.localRotation = Quaternion.identity;

            ObjectSync.FlagDiscontinuity();
            ObjectSync.Respawn();
        }

        public override void OnPickup()
        {
            if (!OwnedByLocal)
                return;

            justPickedUp = true;
            PickupSFX.PlayForAll();
        }

        public override void OnPickupUseDown()
        {
            if (!OwnedByLocal)
                return;

            if (justPickedUp)
                return;

            Rotator.localRotation *= Quaternion.Euler(0f, 180f, 0f);
        }

        public override void OnPickupUseUp() => justPickedUp = false;

        public override void OnDrop()
        {
            if (!OwnedByLocal)
                return;

            if (ParentPlayer.ActionsAllowed && InArea)
            {
                ParentPlayer.Manager.AddToConsole("Card choice confirmed");
                ParentPlayer.OnCardActionChosen();
                ParentPlayer.AddtBet.Pickup.Drop();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.isTrigger && !Pickup.IsHeld && (transform.position - lastPosition).sqrMagnitude > MinimumPutdownDistance * MinimumPutdownDistance)
            {
                PutdownSFX.Play();
                lastPosition = transform.position;
            }
        }

        public override void Deserialize()
        {
            // Receive the cards owned by the player that are for their hand
            if (OwnedByLocal)
                Pickup.pickupable = !ParentPlayer.Manager.Processing && !(cardIdx1 == -1 && cardIdx2 == -1);
            // Otherwise only show the other players that they have cards if there's an owner here
            else
            {
                if (HasOwner && (cardIdx1 != -1 || cardIdx2 != -1))
                {
                    if (card1Comp == null)
                    {
                        GameObject card1Obj = ParentPlayer.Manager.GameDeck.GetCardOf(OwnerID, cardIdx1);

                        if (card1Obj != null)
                        {
                            card1Comp = card1Obj.GetComponent<Card>();
                            card1Comp.Deserialize();
                        }
                    }
                    if (card2Comp == null)
                    {
                        GameObject card2Obj = ParentPlayer.Manager.GameDeck.GetCardOf(OwnerID, cardIdx2);

                        if (card2Obj != null)
                        {
                            card2Comp = card2Obj.GetComponent<Card>();
                            card2Comp.Deserialize();
                        }
                    }
                }
                else
                {
                    card1Comp = null;
                    card2Comp = null;
                }

                Pickup.pickupable = false;
            }
        }
    }
}
