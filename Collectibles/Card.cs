// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;

namespace ThisIsBennyK.TexasHoldEm
{
    [RequireComponent(typeof(SortingGroup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Card : Benscript
    {
        public const int Flop1 = 0, Flop2 = 1, Flop3 = 2, Turn = 3, River = 4;

        public const int Spades = 0, Hearts = 1, Clubs = 2, Diamonds = 3, Suits = 4;
        public const int LowAce = 0, Two = 1, Three = 2, Four = 3, Five = 4, Six = 5, Seven = 6, Eight = 7, Nine = 8, Ten = 9, Jack = 10, Queen = 11, King = 12, HighAce = 13, Ranks = 13;

        public const char SpadesBit = (char)(1 << Spades), HeartsBit = (char)(1 << Hearts), ClubsBit = (char)(1 << Clubs), DiamondsBit = (char)(1 << Diamonds);

        public const byte DeckTarget = 0, PlayerTarget = 1, CommCardTarget = 2;

        public readonly string[] SuitNames =
        {
            "Spades",
            "Hearts",
            "Clubs",
            "Diamonds"
        };

        public readonly string[] RankNames =
        {
            "A",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "J",
            "Q",
            "K",
        };

        public const float Decay = 16.0f;

        [Header("Card Parts (Must not be relatives)")]

        public MeshRenderer CardFace;
        //public Vector2 CardFacePosition, CardFaceOffset;
        
        public MeshRenderer CardMesh;

        [Header("Card Data")]

        public Deck ParentDeck;

        [Range(Spades, Diamonds)]
        public int Suit;
        [Range(LowAce, King)]
        public int Rank;

        [HideInInspector]
        public int Index = -1;

        public string FaceName => RankNames[Rank] + " of " + SuitNames[Suit];

        public override void Start()
        {
            base.Start();

            //Vector2 scaledOffset = new Vector2(CardFaceOffset.x * Rank, CardFaceOffset.y * Suit);
            //Vector2 finalOffset = CardFacePosition + scaledOffset;

            //CardFace.transform.localPosition = new Vector3(finalOffset.x, finalOffset.y, CardFace.transform.localPosition.z);
            //CardFace.gameObject.GetComponent<Animator>().Play($"Card {Suit * Ranks + Rank + 1}");
            //CardFace.GetComponent<Animator>().SetTrigger($"Card {Suit * Ranks + Rank + 1}");
            _SetCardFront();
        }

        private void Update() => UpdateCardProperties();

        private void UpdateCardProperties()
        {
            Transform xform;

            if (ParentDeck.Manager.GameStarted && HasOwner)
            {
                if (Index == ParentDeck.Manager.Flop1CardIdx)
                {
                    xform = ParentDeck.Manager.CommunityCardAnimators[Flop1].ReferencePoint.transform;

                    CardFace.enabled = ParentDeck.Manager.CurStreet >= GameManager.FlopStreet;
                    CardMesh.gameObject.SetActive(ParentDeck.Manager.CurStreet >= GameManager.FlopStreet);
                }
                else if (Index == ParentDeck.Manager.Flop2CardIdx)
                {
                    xform = ParentDeck.Manager.CommunityCardAnimators[Flop2].ReferencePoint.transform;

                    CardFace.enabled = ParentDeck.Manager.CurStreet >= GameManager.FlopStreet;
                    CardMesh.gameObject.SetActive(ParentDeck.Manager.CurStreet >= GameManager.FlopStreet);
                }
                else if (Index == ParentDeck.Manager.Flop3CardIdx)
                {
                    xform = ParentDeck.Manager.CommunityCardAnimators[Flop3].ReferencePoint.transform;

                    CardFace.enabled = ParentDeck.Manager.CurStreet >= GameManager.FlopStreet;
                    CardMesh.gameObject.SetActive(ParentDeck.Manager.CurStreet >= GameManager.FlopStreet);
                }
                else if (Index == ParentDeck.Manager.TurnCardIdx)
                {
                    xform = ParentDeck.Manager.CommunityCardAnimators[Turn].ReferencePoint.transform;

                    CardFace.enabled = ParentDeck.Manager.CurStreet >= GameManager.TurnStreet;
                    CardMesh.gameObject.SetActive(ParentDeck.Manager.CurStreet >= GameManager.TurnStreet);
                }
                else if (Index == ParentDeck.Manager.RiverCardIdx)
                {
                    xform = ParentDeck.Manager.CommunityCardAnimators[River].ReferencePoint.transform;

                    CardFace.enabled = ParentDeck.Manager.CurStreet >= GameManager.RiverStreet;
                    CardMesh.gameObject.SetActive(ParentDeck.Manager.CurStreet >= GameManager.RiverStreet);
                }
                else if (ParentDeck.Manager.PlayerHasJoined(OwnerID, true))
                {
                    Player player = ParentDeck.Manager.GetPlayerObject(OwnerID);

                    if (player.LateJoiner)
                        xform = ParentDeck.transform;
                    else if (Index == player.Hand.FirstCard)
                        xform = player.Hand.Card1Syncer;
                    else
                        xform = player.Hand.Card2Syncer;

                    if (player.LateJoiner)
                    {
                        CardFace.enabled = false;
                        CardMesh.gameObject.SetActive(false);
                    }
                    else if (player.OwnedByLocal)
                    {
                        CardFace.enabled = true;
                        CardMesh.gameObject.SetActive(true);
                    }
                    else
                    {
                        CardFace.enabled = ParentDeck.Manager._CheckedAtShowdown(player.PlayerNum);
                        CardMesh.gameObject.SetActive(true);
                    }
                }
                else
                {
                    xform = ParentDeck.transform;

                    CardFace.enabled = false;
                    CardMesh.gameObject.SetActive(false);
                }
            }
            else
            {
                xform = ParentDeck.transform;

                CardFace.enabled = false;
                CardMesh.gameObject.SetActive(false);
            }

            transform.SetPositionAndRotation(xform.transform.position, xform.transform.rotation);
        }

        public void ChangeDesign(uint design)
        {
            //CardFace.sprite = ParentDeck.CardDesignSprites[design];
            CardFace.material = ParentDeck.CardFrontMaterials[design];
            CardMesh.material = ParentDeck.CardBackMaterials[design];
        }

        public void _SetCardFront() => CardFace.GetComponent<Animator>().SetTrigger($"Card {Suit * Ranks + Rank + 1}");

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return true;
        }
    }
}
