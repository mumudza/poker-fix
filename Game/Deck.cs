// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDK3.UdonNetworkCalling;  

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Deck : Benscript
    {
        private readonly string[] BuiltInCreditStrings =
        {
              "<size=16>Built-in design by:</size>\n"
            , "<size=16>Modèles intégrés créés par :</size>\n"
            , "<size=16>Diseño integrado por:</size>\n"
            , "<size=16>Eingebautes Design von:</size>\n"
            , "<size=16>による内蔵デザイン：</size>\n"
            , "<size=16>기본 제공 디자인 작성자:</size>\n"
            , "<size=16>Sisäänrakennettu suunnittelu:</size>\n"
        };
        private readonly string[] AccessibilityOptionStrings =
        {
              "<size=16>Accessibility Option</size>\n"
            , "<size=16>Option d'Accessibilité</size>\n"
            , "<size=16>Opción de accesibilidad</size>\n"
            , "<size=16>Option Zugänglichkeit</size>\n"
            , "<size=16>アクセシビリティ・オプション</size>\n"
            , "<size=16>접근성 옵션</size>\n"
            , "<size=16>Esteettömyysvaihtoehto</size>\n"
        };
        private readonly string[] CustomCreditStrings =
        {
              "<size=16>Custom design by:</size>\n"
            , "<size=16>Modèles sur-mesure créés par :</size>\n"
            , "<size=16>Diseño personalizado por:</size>\n"
            , "<size=16>Kundenspezifisches Design von:</size>\n"
            , "<size=16>によるカスタムデザイン：</size>\n"
            , "<size=16>커스텀 디자인 작성자:</size>\n"
            , "<size=16>Mukautettu suunnittelu:</size>\n"
        };
        private readonly string[] EasyReadingStrings =
        {
              "Easy Reading"
            , "Lecture facile"
            , "Lectura fácil"
            , "Leichtes Lesen"
            , "イージー・リーディング"
            , "쉽게 읽기"
            , "Helposti Luettava"
        };

        [Header("Deck Parts (Must be relatives)")]

        public SyncedObjectPool pool;

        [Header("Deck Parts (Must not be relatives)")]

        public GameManager Manager;
        public CommunityCardInfoDisplay CommCardInfoDisplay;
        public MeshRenderer DeckModel;

        [Header("Card Designs")]

        public Material[] BuiltinCardFrontMaterials;
        public Material[] BuiltinCardBackMaterials;
        public Sprite[] BuiltinCardPreviewSprites;
        public string[] BuiltinCardDesignCredits;

        public Material[] CustomCardFrontMaterials;
        public Material[] CustomCardBackMaterials;
        public Sprite[] CustomCardPreviewSprites;
        public string[] CustomCardDesignCredits;

        public Material[] CardFrontMaterials
        {
            get
            {
                if (allFrontMaterials == null)
                {
                    int customLength = CustomCardFrontMaterials == null ? 0 : CustomCardFrontMaterials.Length;

                    allFrontMaterials = new Material[BuiltinCardFrontMaterials.Length + customLength];

                    for (int i = 0; i < BuiltinCardFrontMaterials.Length; ++i)
                        allFrontMaterials[i] = BuiltinCardFrontMaterials[i];

                    for (int i = 0; i < customLength; ++i)
                        allFrontMaterials[BuiltinCardFrontMaterials.Length + i] = CustomCardFrontMaterials[i];
                }

                return allFrontMaterials;
            }
        }

        public Material[] CardBackMaterials
        {
            get
            {
                if (allBackMaterials == null)
                {
                    int customLength = CustomCardBackMaterials == null ? 0 : CustomCardBackMaterials.Length;

                    allBackMaterials = new Material[BuiltinCardBackMaterials.Length + CustomCardBackMaterials.Length];

                    for (int i = 0; i < BuiltinCardBackMaterials.Length; ++i)
                        allBackMaterials[i] = BuiltinCardBackMaterials[i];

                    for (int i = 0; i < customLength; ++i)
                        allBackMaterials[BuiltinCardBackMaterials.Length + i] = CustomCardBackMaterials[i];
                }

                return allBackMaterials;
            }
        }

        public Sprite[] CardPreviewSprites
        {
            get
            {
                if (allPreviewSprites == null)
                {
                    int customLength = CustomCardPreviewSprites == null ? 0 : CustomCardPreviewSprites.Length;

                    allPreviewSprites = new Sprite[BuiltinCardPreviewSprites.Length + customLength];

                    for (int i = 0; i < BuiltinCardPreviewSprites.Length; ++i)
                        allPreviewSprites[i] = BuiltinCardPreviewSprites[i];

                    for (int i = 0; i < customLength; ++i)
                        allPreviewSprites[BuiltinCardPreviewSprites.Length + i] = CustomCardPreviewSprites[i];
                }

                return allPreviewSprites;
            }
        }

        public string[] CardDesignCredits
        {
            get
            {
                if (allDesignCredits == null)
                {
                    int customLength = CustomCardDesignCredits == null ? 0 : CustomCardDesignCredits.Length;

                    allDesignCredits = new string[Translatable.NumLangs][];

                    for (int lang = (int)Translatable.LangEN; lang < (int)Translatable.NumLangs; ++lang)
                    {
                        allDesignCredits[lang] = new string[BuiltinCardDesignCredits.Length + customLength];

                        for (int i = 0; i < BuiltinCardPreviewSprites.Length; ++i)
                        {
                            string credit = BuiltinCardDesignCredits[i];

                            if (credit == "Easy Reading")
                                credit = AccessibilityOptionStrings[lang] + EasyReadingStrings[lang];
                            else
                                credit = BuiltInCreditStrings[lang] + credit;

                            allDesignCredits[lang][i] = credit;
                        }

                        for (int i = 0; i < customLength; ++i)
                            allDesignCredits[lang][BuiltinCardDesignCredits.Length + i] = CustomCreditStrings[lang] + CustomCardDesignCredits[i];
                    }
                }

                return allDesignCredits[Manager.LocalSettings.LanguageIndex];
            }
        }

        private Material[] allFrontMaterials;
        private Material[] allBackMaterials;
        private Sprite[] allPreviewSprites;
        private string[][] allDesignCredits;

        public bool AllReturned
        {
            get
            {
                for (int i = 0; i < pool.Objects.Length; ++i)
                {
                    if (pool.IsOwned(i) || pool.Objects[i].GetComponent<Card>().HasOwner)
                        return false;
                }
                return true;
            }
        }

        public override void Start()
        {
            base.Start();

            for (int i = 0; i < pool.Objects.Length; ++i)
                pool.Objects[i].GetComponent<Card>().Index = i;
        }

        public void Shuffle() => pool.Shuffle();

        public void DrawCardsFor(VRCPlayerApi player, int numCards) => pool.RequestObjectsFor(player, numCards);

        public void DrawCardsForPlayer(VRCPlayerApi player, int numCards)
        {
            // Use the existing generic pool functionality
            pool.RequestObjectsFor(player, numCards);            
        }

        public void NotifyPlayersOfCardOwnership()
        {
            // Go through all players and notify them if they have new cards
            foreach (Player player in Manager.Players)
            {
                if (player.HasOwner)
                {
                    int[] cardIndices = GetCardIndicesOf(player.OwnerID);
                    if (cardIndices != null && cardIndices.Length > 0)
                    {
                        // Only send if we have the right number of cards for a hand
                        if (cardIndices.Length >= Hand.Size)
                        {
                            // Filter out community cards to get only hole cards
                            int[] holeCardIndices = GetHoleCardIndices(cardIndices);
                            
                            if (holeCardIndices != null && holeCardIndices.Length >= Hand.Size)
                            {
                                var onCardsReadyData = new DataDictionary();
                                var indicesData = new DataList();
                                foreach (int idx in holeCardIndices)
                                {
                                    indicesData.Add(idx);
                                }
                                onCardsReadyData.Add("indices", indicesData);

                                // Send event to the specific player
                                player.Hand.SendToOwnerWithParam(
                                    nameof(player.Hand.OnCardsReady), 
                                    onCardsReadyData
                                );
                                
                                Debug.Log($"Notified player {player.PlayerNum} of cards: {holeCardIndices[0]}, {holeCardIndices[1]}");
                            }
                            else
                            {
                                Debug.LogWarning($"Player {player.PlayerNum} doesn't have enough hole cards: {holeCardIndices}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Player {player.PlayerNum} has insufficient card count: {cardIndices.Length}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Player {player.PlayerNum} has no card ownership data");
                    }
                }
            }
        }

        private int[] GetHoleCardIndices(int[] allCardIndices)
        {
            if (allCardIndices == null || allCardIndices.Length == 0)
                return null;

            Debug.Log($"GetHoleCardIndices: Processing {allCardIndices.Length} card indices for player");

            // Filter out community cards to get only hole cards
            int[] holeCards = new int[Hand.Size];
            int holeCardCount = 0;

            foreach (int cardIdx in allCardIndices)
            {
                Debug.Log($"Checking card index {cardIdx}: IsCommunityCard={Manager.IsCommunityCard(cardIdx)}");
                
                if (!Manager.IsCommunityCard(cardIdx))
                {
                    if (holeCardCount < Hand.Size)
                    {
                        holeCards[holeCardCount] = cardIdx;
                        holeCardCount++;
                        Debug.Log($"Added hole card {holeCardCount}: index {cardIdx}");
                    }
                    else
                    {
                        // Player has more than Hand.Size cards, which shouldn't happen
                        Debug.LogError($"Player has more than {Hand.Size} hole cards. Total cards: {allCardIndices.Length}, Hole cards found: {holeCardCount}");
                        break;
                    }
                }
            }

            // If we found the right number of hole cards, return them
            if (holeCardCount == Hand.Size)
            {
                Debug.Log($"Successfully found {Hand.Size} hole cards: [{holeCards[0]}, {holeCards[1]}]");
                return holeCards;
            }
            else
            {
                Debug.LogWarning($"Expected {Hand.Size} hole cards but found {holeCardCount} from {allCardIndices.Length} total cards");
                return null;
            }
        }

        public int[] GetCardIndices() => pool.GetOwnedObjectIndicesFor(LocalID);
        public int[] GetCardIndicesOf(int playerID) => pool.GetOwnedObjectIndicesFor(playerID);

        public GameObject GetCard(int idx) => pool.TryToGetObjectFor(LocalID, idx);
        public GameObject GetCardOf(int playerID, int idx) => pool.TryToGetObjectFor(playerID, idx);

        public void ReturnAll()
        {
            foreach (GameObject obj in pool.Objects)
                obj.GetComponent<Card>().SendToOwner(nameof(Disown));

            pool.ReturnAll();
        }

        public void ClaimAllUndrawnCards()
        {
            for (int i = 0; i < pool.Objects.Length; ++i)
            {
                if (!pool.IsOwned(i))
                    pool.Objects[i].GetComponent<Card>().SendToOwner(nameof(Disown));
            }
        }

        public void ChangeDesigns(uint design)
        {
            foreach (GameObject obj in pool.Objects)
                obj.GetComponent<Card>().ChangeDesign(design);

            CommCardInfoDisplay.ChangeDesign(design);

            DeckModel.material = CardBackMaterials[design];
        }
    }
}
