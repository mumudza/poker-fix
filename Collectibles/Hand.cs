// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDK3.UdonNetworkCalling;  

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

        public void SetCardIndices(int idx1, int idx2)
        {
            cardIdx1 = idx1;
            cardIdx2 = idx2;
        }

        public override void Start()
        {
            lastPosition = transform.position;
            base.Start();
        }

        public void Update()
        {
            // Cards are now received via events instead of polling in Update()
            // This eliminates race conditions and improves performance
            
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

        [NetworkCallable]
        public void OnCardsReady(string json)
        {
            Debug.Log($"=== Player {ParentPlayer.PlayerNum} OnCardsReady ===");
            Debug.Log($"Received JSON: {json}");
            
            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                // Deserialization succeeded! Let's figure out what we've got.
                if (result.TokenType == TokenType.DataDictionary)
                {
                    Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                    // Use the passed status instead of accessing local field
                    DataList indices = result.DataDictionary["indices"].DataList;

                    if (ParentPlayer.LateJoiner)
                    {
                        Debug.Log($"Player {ParentPlayer.PlayerNum} is a late joiner, skipping card setup");
                        return;
                    }
                                            
                    if (indices == null || indices.Count < Hand.Size)
                    {
                        Debug.LogError($"Player {ParentPlayer.PlayerNum} received invalid card indices: expected {Hand.Size}, got {(indices != null ? indices.Count : 0)}");
                        // Don't request cards again - this could cause infinite loops
                        // Instead, log the error and return
                        return;
                    }
                    
                    // Safely extract and validate card indices
                    double newCardIdx1Double = indices[0].Double;
                    double newCardIdx2Double = indices[1].Double;

                    int newCardIdx1 = (int)newCardIdx1Double;
                    int newCardIdx2 = (int)newCardIdx2Double;
                    
                    Debug.Log($"Player {ParentPlayer.PlayerNum} received card indices: card1={newCardIdx1}, card2={newCardIdx2}");
                    
                    // Validate card indices before assigning them
                    if (newCardIdx1 == -1 || newCardIdx2 == -1)
                    {
                        Debug.LogError($"Player {ParentPlayer.PlayerNum} received invalid card indices: card1={newCardIdx1}, card2={newCardIdx2}");
                        return;
                    }
                    
                    if (newCardIdx1 == newCardIdx2)
                    {
                        Debug.LogError($"Player {ParentPlayer.PlayerNum} received duplicate card indices: both={newCardIdx1}");
                        return;
                    }
                    
                    // Assign validated card indices
                    cardIdx1 = newCardIdx1;
                    cardIdx2 = newCardIdx2;
                    
                    Debug.Log($"Player {ParentPlayer.PlayerNum} assigned card indices: card1={cardIdx1}, card2={cardIdx2}");
                    
                    // Now safely get and setup the card objects
                    SetupCardObjects();

                    // Send completion event to GameManager using proper event communication
                    var completionData = new DataDictionary();
                    completionData.Add("playerNum", ParentPlayer.PlayerNum);
                    completionData.Add("card1Idx", cardIdx1);
                    completionData.Add("card2Idx", cardIdx2);
                    completionData.Add("ready", true);
                    
                    Debug.Log($"Player {ParentPlayer.PlayerNum} sending hand ready event to GameManager");
                    
                    // Send event directly to GameManager instead of trying to execute callback string
                    ParentPlayer.Manager.SendToOwnerWithParam(nameof(GameManager.OnPlayerHandReady), completionData);
                    Serialize();

                }
                else 
                {
                    Debug.LogError($"Unexpected result when deserializing json {json}");
                }
            } else {
                // Deserialization failed. Let's see what the error was.
                Debug.LogError($"OnCardsReady: Failed to Deserialize json {json} - {result.ToString()}");
            }
            
            Debug.Log($"=== Player {ParentPlayer.PlayerNum} OnCardsReady complete ===");
        }

        public void SetupCardObjects()
        {
            Debug.Log($"=== Player {ParentPlayer.PlayerNum} SetupCardObjects ===");
            Debug.Log($"Setting up cards: cardIdx1={cardIdx1}, cardIdx2={cardIdx2}, OwnerID={OwnerID}");
            
            // Reset card components first
            card1Comp = null;
            card2Comp = null;
            
            // Get card 1
            if (cardIdx1 != -1)
            {
                GameObject card1Obj = ParentPlayer.Manager.GameDeck.GetCardOf(OwnerID, cardIdx1);
                if (card1Obj != null)
                {
                    card1Comp = card1Obj.GetComponent<Card>();
                    if (card1Comp != null)
                    {
                        card1Comp.OwnByLocal();
                        card1Comp.Deserialize();
                        Debug.Log($"Player {ParentPlayer.PlayerNum} successfully set up card 1 (index {cardIdx1})");
                    }
                    else
                    {
                        Debug.LogError($"Player {ParentPlayer.PlayerNum} failed to get Card component for index {cardIdx1}");
                    }
                }
                else
                {
                    Debug.LogError($"Player {ParentPlayer.PlayerNum} failed to get card object for index {cardIdx1}");
                }
            }
            else
            {
                Debug.LogWarning($"Player {ParentPlayer.PlayerNum} has invalid card index 1: {cardIdx1}");
            }
            
            // Get card 2
            if (cardIdx2 != -1)
            {
                GameObject card2Obj = ParentPlayer.Manager.GameDeck.GetCardOf(OwnerID, cardIdx2);
                if (card2Obj != null)
                {
                    card2Comp = card2Obj.GetComponent<Card>();
                    if (card2Comp != null)
                    {
                        card2Comp.OwnByLocal();
                        card2Comp.Deserialize();
                        Debug.Log($"Player {ParentPlayer.PlayerNum} successfully set up card 2 (index {cardIdx2})");
                    }
                    else
                    {
                        Debug.LogError($"Player {ParentPlayer.PlayerNum} failed to get Card component for index {cardIdx2}");
                    }
                }
                else
                {
                    Debug.LogError($"Player {ParentPlayer.PlayerNum} failed to get card object for index {cardIdx2}");
                }
            }
            else
            {
                Debug.LogWarning($"Player {ParentPlayer.PlayerNum} has invalid card index 2: {cardIdx2}");
            }
            
            // Validate that we have both cards
            if (card1Comp == null || card2Comp == null)
            {
                Debug.LogError($"Player {ParentPlayer.PlayerNum} failed to set up complete hand. Card1: {(card1Comp != null ? "OK" : "MISSING")}, Card2: {(card2Comp != null ? "OK" : "MISSING")}");
            }
            else
            {
                Debug.Log($"Player {ParentPlayer.PlayerNum} successfully set up complete hand with cards: {cardIdx1}, {cardIdx2}");
            }
            
            Debug.Log($"=== Player {ParentPlayer.PlayerNum} SetupCardObjects complete ===");
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
            {
                bool shouldPickupable = !ParentPlayer.Manager.Processing && !(cardIdx1 == -1 && cardIdx2 == -1);
                Pickup.pickupable = shouldPickupable;
            }
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
