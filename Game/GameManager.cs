// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GameManager : Benscript
    {
        public const int CommunityCardSize = 5;
        public const int InvalidStreet = -1, PreflopStreet = 0, FlopStreet = 1, TurnStreet = 2, RiverStreet = 3, ShowdownStreet = 4, NumStreets = 5;
        // The maximum number of ties in any given round
        // REMARK: Based on the most extreme scenario: suppose the community cards are a royal flush.
        // This lends the possibility of a number of players tying by having cards of the exact same rank.
        // The greatest number of players that can tie like this is capped by the maximum number of suits.
        public const int MaxTies = Card.Suits;
        // Forced bet amounts
        // REMARK: Hardcoded as a design decision for now
        public const int SmallBlind = 100, BigBlind = 200;

        public const byte NoStatus = 0, CheckedStatus = 1, BettedStatus = 2, FoldedStatus = 3;

        public const string NoHand = "---";

        public readonly string[][] HandRanks =
        {
            new string[] {
                "High Card",
                "One Pair",
                "Two Pair",
                "Three of a Kind",
                "Straight",
                "Flush",
                "Full House",
                "Four of a Kind",
                "Straight Flush",
                "Royal Flush"
            },
            new string[] {
                "Carte haute",
                "Paire",
                "Double paire",
                "Brelan",
                "Quinte",
                "Couleur",
                "Full",
                "Carré",
                "Quinte flush",
                "Quinte flush royale"
            },
            new string[] {
                "Carta alta",
                "Pareja",
                "Doble pareja",
                "Trío",
                "Escalera",
                "Color",
                "Full",
                "Escalera de color",
                "Póquer",
                "Escalera real"
            },
            new string[] {
                "Höchste Karte",
                "Ein Paar",
                "Zwei Paare",
                "Drilling",
                "Straße",
                "Flush",
                "Full House",
                "Vierling",
                "Straight Flush",
                "Royal Flush"
            },
            new string[] {
                "ノーペア",
                "ワンペア",
                "ツーペア",
                "スリーカード",
                "ストレート",
                "フラッシュ",
                "フルハウス",
                "フォーカード",
                "ストレートフラッシュ",
                "ロイヤル・フラッシュ"
            },
            new string[] {
                "하이 카드",
                "원 페어",
                "투 페어",
                "쓰리 오브 어 카인드",
                "스트레이트",
                "플러시",
                "풀하우스",
                "포 오브 어 카인드",
                "스트레이트 플러시",
                "로얄 플러시"
            },
            new string[] {
                "Hai",
                "Pari",
                "Kaksi Paria",
                "Kolmoset",
                "Suora",
                "Väri",
                "Täyskäsi",
                "Neloset",
                "Värisuora",
                "Kuningasvärisuora"
            }
        };

        public const int TiesKey = 0, PlayerRankingKey = 1;

        public const int MinPlayersForVoting = 2;

        public const byte
              BBSGameStarted = 0
            , BBSProcessing = 1
            , BBSWaitingForNextGame = 2
            , BBSWaitingForNextRound = 3
            , BBSWaitingForPlayersDetermined = 4
            , BBSWaitingForCardsReturned = 5
            , BBSRoundEnded = 6
            , BBSMidgameJoining = 7;

        [Header("Game Parts (Must be relatives)")]

        public Deck GameDeck;
        public CommunityCardAnimator[] CommunityCardAnimators;
        public ActionArea ActionArea;
        public CommunityCardInfoDisplay CommCardInfoDisplay;

        [Header("Game Parts (Must not be relatives)")]

        [Tooltip("The manager of this table. (Be sure to put this table in the list of tables of the multimanager.)")]
        public THMMultiManager MultiManager;

        public LocalSettings LocalSettings;
        public Player[] Players;
        public InGameInfoDisplay InGameInfo;
        public UnstartedInfoDisplay UnstartedInfo;
        public TMPro.TextMeshProUGUI OptionalConsole;
        public OutsiderLocalSettings[] OutsiderLocalSettingsPanels;
        public SoundPool Music;
        public SFXPlayer FoldJingle, CheckJingle, CallJingle, RaiseJingle, AllInJingle, RevealJingle, WinLongJingle, WinShortJingle, TurnJingle;
        public SoundPool TwinkleSFX;

        [Header("Game Properties")]

        [UdonSynced]
        [Range(Bankroll.Minimum, Bankroll.Maximum)]
        public int StartingBankrolls;

        public float LongRoundEndTime = 8f;
        public float ShortRoundEndTime = 5f;

        [UdonSynced]
        private byte boolBitSet = 0b_1000_0000;

        private bool gameStarted
        {
            get => (boolBitSet & (1 << BBSGameStarted)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSGameStarted)) | ((value ? 1 : 0) << BBSGameStarted));
        }

        [UdonSynced]
        private int curPlayerIdx = 0;

        [UdonSynced]
        private int curDealerIdx = 0;

        [UdonSynced]
        private int flop1CardIdx = -1;
        [UdonSynced]
        private int flop2CardIdx = -1;
        [UdonSynced]
        private int flop3CardIdx = -1;
        [UdonSynced]
        private int turnCardIdx = -1;
        [UdonSynced]
        private int riverCardIdx = -1;

        [UdonSynced]
        private int[] bestFlopHands;
        [UdonSynced]
        private long[] bestFlopScores;
        [UdonSynced]
        private long[] bestFlopKickers;
        [UdonSynced]
        private int[] bestTurnHands;
        [UdonSynced]
        private long[] bestTurnScores;
        [UdonSynced]
        private long[] bestTurnKickers;
        [UdonSynced]
        private int[] bestRiverHands;
        [UdonSynced]
        private long[] bestRiverScores;
        [UdonSynced]
        private long[] bestRiverKickers;

        // The start of the jump table. This is the player index of the first found player with the best hand
        [UdonSynced]
        private int firstWinner = Player.InvalidNum;
        // A jump table where each entry is the player with the next best hand
        // Ex. secondWinner = winRankings[firstWinner], thirdWinner = winRankings[secondWinner], etc.
        [UdonSynced]
        private int[] winRankings;

        [UdonSynced]
        private int curRound = 0;

        [UdonSynced]
        private int curStreet = InvalidStreet;

        [UdonSynced]
        private ushort curStatuses;

        // Indices of players who are limiting the size of each pot
        // InvalidNum means the pot has no limit
        [UdonSynced]
        private int[] potLimiters;

        // Amount of money pot is limited to
        [UdonSynced]
        private int[] potLimits;

        // Number of players participating in each pot
        [UdonSynced]
        private int[] participantsPerPot;

        private bool processing
        {
            get => (boolBitSet & (1 << BBSProcessing)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSProcessing)) | ((value ? 1 : 0) << BBSProcessing));
        }

        private bool waitingForNextGame
        {
            get => (boolBitSet & (1 << BBSWaitingForNextGame)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSWaitingForNextGame)) | ((value ? 1 : 0) << BBSWaitingForNextGame));
        }

        private bool waitingForNextRound
        {
            get => (boolBitSet & (1 << BBSWaitingForNextRound)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSWaitingForNextRound)) | ((value ? 1 : 0) << BBSWaitingForNextRound));
        }

        private bool waitingForPlayersDetermined
        {
            get => (boolBitSet & (1 << BBSWaitingForPlayersDetermined)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSWaitingForPlayersDetermined)) | ((value ? 1 : 0) << BBSWaitingForPlayersDetermined));
        }

        private bool waitingForCardsReturned
        {
            get => (boolBitSet & (1 << BBSWaitingForCardsReturned)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSWaitingForCardsReturned)) | ((value ? 1 : 0) << BBSWaitingForCardsReturned));
        }

        private bool roundEnded
        {
            get => (boolBitSet & (1 << BBSRoundEnded)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSRoundEnded)) | ((value ? 1 : 0) << BBSRoundEnded));
        }

        [UdonSynced]
        private string prevWinners = "";

        private bool midgameJoining
        {
            get => (boolBitSet & (1 << BBSMidgameJoining)) != 0;
            set => boolBitSet = (byte)((boolBitSet & ~(1 << BBSMidgameJoining)) | ((value ? 1 : 0) << BBSMidgameJoining));
        }

        [UdonSynced]
        private float roundEndTimer = 0f;

        private int numRaises = 0;
        private int numAllIns = 0;

        private Translatable[] translatables = null;

        public int NumPlayers
        {
            get
            {
                int result = 0;

                foreach (Player player in Players)
                    result += (player.HasOwner ? 1 : 0);

                return result;
            }
        }
        public int NumPlayersDuringStreet
        {
            get
            {
                int result = 0;

                foreach (Player player in Players)
                    result += PlayerInStreet(player) ? 1 : 0;

                return result;
            }
        }
        public int NumPlayersAfterRound
        {
            get
            {
                int result = 0;

                foreach (Player player in Players)
                    result += (player.HasOwner && player.HasMoney ? 1 : 0);

                return result;
            }
        }
        public int TotalStartingChips => StartingBankrolls * NumPlayers; // TODO: Record # players at start of round
        public bool GameStarted => gameStarted;
        public int CurPlayerIndex => curPlayerIdx;
        public Player CurPlayer => Players[curPlayerIdx];
        public int CurDealerIndex => curDealerIdx;
        public int CurStreet => curStreet;
        public bool Processing => processing;

        public int Flop1CardIdx => flop1CardIdx;
        public int Flop2CardIdx => flop2CardIdx;
        public int Flop3CardIdx => flop3CardIdx;
        public int TurnCardIdx => turnCardIdx;
        public int RiverCardIdx => riverCardIdx;

        public bool HasEnoughPlayers => EnoughPlayersInGame(NumPlayers);

        public bool UsingHeadsUpRules => NumPlayers < 3;

        public int CurGreatestBet
        {
            get
            {
                if (curStreet < 0 || curStreet >= NumStreets)
                    return 0;

                int curGreatestBet = 0;

                foreach (Player player in Players)
                {
                    if (!player.HasOwner || player.LateJoiner)
                        continue;
                    curGreatestBet = Mathf.Max(player.GetBet(curStreet), curGreatestBet);
                }

                return curGreatestBet;
            }
        }

        public int SumOfAllBets
        {
            get
            {
                int sumOfAllBets = 0;

                foreach (Player player in Players)
                {
                    if (!player.HasOwner || player.LateJoiner)
                        continue;

                    sumOfAllBets += player.SumOfAllBets;
                }

                return sumOfAllBets;
            }
        }

        public int CurPot
        {
            get
            {
                int curPot = 0;

                foreach (int playerNum in potLimiters)
                {
                    if (playerNum == Player.InvalidNum)
                        break;
                    ++curPot;
                }

                return curPot;
            }
        }

        public int NumPots => CurPot + 1;

        public bool ReadyToAdvanceStreets
        {
            get
            {
                bool allChecked = true;
                bool allTakenAction = true;
                bool allBetsEqual = true;

                int curGreatestBet = CurGreatestBet;

                for (int i = 0; i < Players.Length; ++i)
                {
                    Player player = Players[i];

                    if (!player.HasOwner || player.LateJoiner || (!player.HasMoney && curStreet != ShowdownStreet) || GetCurStatus(i) == FoldedStatus)
                        continue;

                    if (GetCurStatus(i) != CheckedStatus)
                    {
                        AddToConsole($"P{i} didn't check");
                        allChecked = false;
                    }

                    if (curStreet == PreflopStreet || curStreet == ShowdownStreet)
                    {
                        if (GetCurStatus(i) == NoStatus)
                        {
                            AddToConsole($"P{i} hasn't done anything yet");
                            allTakenAction = false;
                        }
                    }
                    else
                    {
                        if (GetCurStatus(i) != BettedStatus)
                        {
                            AddToConsole($"P{i} hasn't betted");
                            allTakenAction = false;
                        }
                    }

                    if (curStreet > InvalidStreet && curStreet < NumStreets && player.GetBet(curStreet) != curGreatestBet)
                    {
                        AddToConsole($"P{i} doesn't have {curGreatestBet} chips");
                        allBetsEqual = false;
                    }
                }

                return allChecked || (allTakenAction && allBetsEqual);
            }
        }

        // Whether there is one or less players who have the ability to take an action
        public bool OneOrLessActionablePlayers
        {
            get
            {
                int playersRemaining = 0;

                for (int i = 0; i < Players.Length; ++i)
                {
                    Player player = Players[i];

                    if (!player.HasOwner || player.LateJoiner)
                        continue;

                    if (GetCurStatus(i) != FoldedStatus && player.HasMoney)
                        ++playersRemaining;
                }

                return playersRemaining <= 1;
            }
        }

        public bool AllButOneFolded
        {
            get
            {
                if (NumPots > 1)
                    return false;

                int playersRemaining = 0;

                for (int i = 0; i < Players.Length; ++i)
                {
                    Player player = Players[i];

                    if (!player.HasOwner || player.LateJoiner)
                        continue;

                    if (GetCurStatus(i) != FoldedStatus)
                        ++playersRemaining;
                }

                return playersRemaining == 1;
            }
        }

        public bool AllChecked
        {
            get
            {
                for (int i = 0; i < Players.Length; ++i)
                {
                    Player player = Players[i];

                    if (!player.HasOwner || player.LateJoiner || !player.HasMoney || GetCurStatus(i) == FoldedStatus)
                        continue;

                    if (GetCurStatus(i) != CheckedStatus)
                        return false;
                }
                return true;
            }
        }
        public int LastBetterIndex
        {
            get
            {
                int lastBetted = curPlayerIdx;

                for (int i = 0; i < Players.Length; ++i)
                {
                    lastBetted = curPlayerIdx - i;

                    if (lastBetted < 0)
                        lastBetted += Players.Length;

                    Player lastBetter = Players[lastBetted];

                    if (!lastBetter.HasOwner || lastBetter.LateJoiner)
                        continue;

                    if (GetCurStatus(lastBetted) == BettedStatus)
                        break;
                }

                return lastBetted;
            }
        }

        public bool RoundEnded => roundEnded;

        public string PrevWinners => prevWinners;

        public bool OwnerAtTable
        {
            get
            {
                foreach (var player in Players)
                {
                    if (player.HasOwner && player.Owner == Owner)
                        return true;
                }
                return false;
            }
        }

        public int CurRound => curRound;

        public int NumLateJoiners
        {
            get
            {
                int numLateJoiners = 0;

                foreach (var player in Players)
                    numLateJoiners += player.HasOwner && player.LateJoiner ? 1 : 0;

                return numLateJoiners;
            }
        }

        public bool MidgameJoining => midgameJoining;

        public int VoteMajority => (NumPlayers + 1) / 2;

        public int NumPlayersWithAction
        {
            get
            {
                int numNoStatuses = 0;

                for (int i = 0; i < Players.Length; ++i)
                {
                    Player player = Players[i];

                    if (!player.HasOwner || player.LateJoiner)
                        continue;

                    if (GetCurStatus(i) == NoStatus)
                        ++numNoStatuses;
                }

                return numNoStatuses;
            }
        }

        public bool AboutToBeWinByDefault
        {
            get
            {
                int numPlayers = 0, numFolded = 0, numNotFolded = 0;

                for (int i = 0; i < Players.Length; ++i)
                {
                    Player player = Players[i];

                    if (!player.HasOwner || player.LateJoiner)
                        continue;

                    if (GetCurStatus(i) == FoldedStatus)
                        ++numFolded;
                    else
                        ++numNotFolded;

                    ++numPlayers;
                }

                return numNotFolded == 2 && numFolded == numPlayers - numNotFolded;
            }
        }

        public override void Start()
        {
            bestFlopHands = new int[Players.Length];
            bestFlopScores = new long[Players.Length];
            bestFlopKickers = new long[Players.Length];
            bestTurnHands = new int[Players.Length];
            bestTurnScores = new long[Players.Length];
            bestTurnKickers = new long[Players.Length];
            bestRiverHands = new int[Players.Length];
            bestRiverScores = new long[Players.Length];
            bestRiverKickers = new long[Players.Length];
            winRankings = new int[Players.Length];

            // The number of players limiting the pot is 1 main pot + (# players - 2) side pots
            // Therefore there are (# players - 1) pots in total
            potLimiters = new int[Players.Length - 1];
            potLimits = new int[potLimiters.Length];
            participantsPerPot = new int[potLimiters.Length];

            base.Start();
        }

        private void Update()
        {
            if (!OwnedByLocal)
                return;

            if (waitingForNextGame)
                WaitForNextGame();
            if (waitingForNextRound)
                WaitForNextRound();
            if (waitingForPlayersDetermined)
                WaitForPlayersDetermined();
            if (waitingForCardsReturned)
                WaitForCardsReturned();
            if (roundEnded)
            {
                float prevTime = roundEndTimer;

                roundEndTimer -= Time.deltaTime;
                
                if (roundEndTimer <= 0f)
                {
                    Serialize();
                    OnRoundEndedTimeout();
                }
            }
        }

        public void OnEnable()
        {
            foreach (var cardObj in GameDeck.pool.Objects)
                cardObj.GetComponent<Card>()._SetCardFront();

            SendCustomEventDelayedFrames(nameof(_SetGameInfo), 1);
        }

        public void _SetGameInfo()
        {
            if (GameStarted)
                InGameInfo.SetInfo();
            else
                UnstartedInfo.SetInfo();
        }

        public override void Disown()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            Debug.Log($"Who was my owner?: {Owner.displayName}, {OwnerID}");

            base.Disown();

            if (NumPlayers > 0)
            {
                foreach (Player player in Players)
                    Debug.Log($"P{player.PlayerNum}: Name: {player.Owner.displayName}, ID: {player.Owner.playerId}");

                foreach (Player player in Players)
                {
                    if (player.HasOwner && (!GameStarted || (!player.OwnedByLocal && player.HasMoney)))
                    {
                        OwnByOther(player.Owner);
                        break;
                    }
                }
            }
            else
                ResetTable();
        }

        public override void Deserialize()
        {
            base.Deserialize();

            Debug.Log($"Players at table: [{Players[0].HasOwner}, {Players[1].HasOwner}, {Players[2].HasOwner}, {Players[3].HasOwner}, {Players[4].HasOwner}, {Players[5].HasOwner}, {Players[6].HasOwner}, {Players[7].HasOwner}]");

            InGameInfo.gameObject.SetActive(GameStarted);
            UnstartedInfo.gameObject.SetActive(!GameStarted);

            if (GameStarted)
                InGameInfo.SetInfo();
            else
                UnstartedInfo.SetInfo();

            DeserializePlayers();

            if (translatables != null && translatables.Length > 0)
            {
                foreach (Translatable translatable in translatables)
                    translatable.Translate();
            }

            TwinkleSFX.Volume = LocalSettings.SFXVolume;

            CommCardInfoDisplay.Deserialize();
        }

        public void _DeserializeLocally()
        {
            base.Deserialize();

            InGameInfo.gameObject.SetActive(GameStarted);
            UnstartedInfo.gameObject.SetActive(!GameStarted);

            if (GameStarted)
                InGameInfo.SetInfo();
            else
                UnstartedInfo.SetInfo();

            DeserializePlayers();

            if (translatables != null && translatables.Length > 0)
            {
                foreach (Translatable translatable in translatables)
                    translatable.Translate();
            }

            TwinkleSFX.Volume = LocalSettings.SFXVolume;

            CommCardInfoDisplay.Deserialize();
        }

        //public override void OnPlayerJoined(VRCPlayerApi player)
        //{
        //    if (LocalPlayer == player)
        //        Deserialize();
        //}

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // REMARK: We cannot use OwnedByLocal or HasOwner here as a player leaving the world
            // leaves both those variables in flux (technically we know that the master player is the
            // one who owns the objects but for the purposes of the game we don't assume the master
            // player is participating, just receiving messages about the game and world)

            // Restrict the action of refreshing the players in-game to the *owner* of the game manager
            // (who may not even be playing if the actual table owner left the world)

            if (!Networking.IsOwner(gameObject))
                return;

            foreach (Player playerObj in Players)
            {
                // If we found the player whose owner matches the leaving player...
                if (playerObj.OwnerID == player.playerId)
                {
                    // ...force whoever's owning it now to leave
                    playerObj.SendToOwner(nameof(Player.Leave));
                    break;
                }
            }
        }

        public override void OnOwnershipForceTransferred()
        {
            GameDeck.ClaimAllUndrawnCards();

            if (!GameStarted)
                return;

            GameDeck.pool.TransferOwnershipTo(Owner, flop1CardIdx, flop2CardIdx, flop3CardIdx, turnCardIdx, riverCardIdx);

            CommunityCardAnimators[Card.Flop1].GetCard();
            CommunityCardAnimators[Card.Flop2].GetCard();
            CommunityCardAnimators[Card.Flop3].GetCard();
            CommunityCardAnimators[Card.Turn].GetCard();
            CommunityCardAnimators[Card.River].GetCard();
        }

        public void AddToConsole(string text)
        {
            Debug.Log(text);

            if (OptionalConsole == null)
                return;

            OptionalConsole.text += text + "\n";
        }

        private void DeserializePlayers()
        {
            foreach (Player player in Players)
                player.Deserialize();
        }

        public bool PlayerHasJoined(int playerID, bool recursionStop = false)
        {
            if (!recursionStop && MultiManager != null)
                return MultiManager.PlayerHasJoined(playerID);

            foreach (Player player in Players)
            {
                if (playerID == player.OwnerID)
                    return true;
            }
            return false;
        }

        // REMARK: Assumes you've checked that the player is at the table using the function above
        public Player GetPlayerObject(int playerID)
        {
            foreach (Player player in Players)
            {
                if (playerID == player.OwnerID)
                    return player;
            }
            return null;
        }

        public void OnPlayerChangedTable()
        {
            if (MultiManager != null)
                MultiManager.DeserializeOthers(this);
        }

        public void ResetTable()
        {
            // Reset the animations before we disown the community card animators
            CommunityCardAnimators[Card.Flop1].RequestAnimation("Default");
            CommunityCardAnimators[Card.Flop2].RequestAnimation("Default");
            CommunityCardAnimators[Card.Flop3].RequestAnimation("Default");
            CommunityCardAnimators[Card.Turn].RequestAnimation("Default");
            CommunityCardAnimators[Card.River].RequestAnimation("Default");

            base.Disown();

            for (int i = 0; i < Players.Length; ++i)
            {
                Player player = Players[i];

                bestFlopHands[i] = -1;
                bestFlopScores[i] = -1;
                bestFlopKickers[i] = -1;
                bestTurnHands[i] = -1;
                bestTurnScores[i] = -1;
                bestTurnKickers[i] = -1;
                bestRiverHands[i] = -1;
                bestRiverScores[i] = -1;
                bestRiverKickers[i] = -1;

                winRankings[i] = Player.InvalidNum;

                if (player.HasOwner)
                    player.SendToOwner(nameof(Player.Leave));
            }

            curStatuses = 0;

            for (int i = 0; i < potLimiters.Length; ++i)
                potLimiters[i] = Player.InvalidNum;
            // No need to clear the pot limits or participants since they are tied to the pot limiters

            GameDeck.ReturnAll();

            gameStarted = false;

            curPlayerIdx = 0;

            curDealerIdx = 0;

            flop1CardIdx = -1;
            flop2CardIdx = -1;
            flop3CardIdx = -1;
            turnCardIdx = -1;
            riverCardIdx = -1;

            firstWinner = Player.InvalidNum;

            curRound = 0;
            curStreet = InvalidStreet;

            processing = false;
            waitingForNextGame = false;
            waitingForNextRound = false;
            waitingForPlayersDetermined = false;
            waitingForCardsReturned = false;
            roundEnded = false;

            Serialize();

            SendToAll(nameof(ResetRaisePitch));
            SendToAll(nameof(ResetAllInPitch));
        }

        private byte GetCurStatus(int i) => (byte)((curStatuses >> (i << 1)) & 0b11);
        private void SetCurStatus(int i, byte status) => curStatuses = (ushort)((ushort)(curStatuses & ~(0b11 << (i << 1))) | (status << (i << 1)));

        public void UpdateJoinedPlayers()
        {
            //Serialize();
            
            foreach (Player player in Players)
                player.ModerationPanel.SendToOwner(nameof(ModerationPanel.ClearVotesForNewJoiners));

            SendToAll(nameof(Deserialize));
        }

        public void StartGame()
        {
            processing = true;
            gameStarted = true;

            // The owner of the table must be a player as well, so get their player index

            int curOwnerIdx = InvalidPlayerID;

            foreach (Player player in Players)
            {
                if (player.OwnedByLocal)
                {
                    curOwnerIdx = player.PlayerNum;
                    break;
                }
            }

            curPlayerIdx = curOwnerIdx;
            curDealerIdx = curOwnerIdx - 1 < 0 ? Players.Length - 1 : curOwnerIdx - 1;

            AddPostSerialListener(nameof(PerformStartOfGameTasks));
            Serialize();
        }

        public void PerformStartOfGameTasks()
        {
            foreach (Player player in Players)
            {
                if (player.HasOwner)
                    player.SendToOwner(nameof(Player.PerformStartOfGameTasks));
            }

            waitingForNextGame = true;
            Serialize();
        }

        private void WaitForNextGame()
        {
            bool allReady = true;

            foreach (Player player in Players)
            {
                if (!player.HasOwner)
                    continue;

                if (!player.HasMoney)
                {
                    allReady = false;
                    break;
                }
            }

            AddToConsole($"Everybody ready for next game? {allReady}");

            if (allReady)
            {
                waitingForNextGame = false;
                Serialize();

                GoToNextRound();
            }
        }

        public void GoToNextRound()
        {
            processing = true;
            roundEnded = false;
            curStreet = InvalidStreet;

            ClearPots();
            CommCardInfoDisplay.SendToAll(nameof(CommunityCardInfoDisplay.ResetOpacities));

            AddPostSerialListener(nameof(DetermineRoundPlayers));
            Serialize();
        }

        public void DetermineRoundPlayers()
        {
            waitingForPlayersDetermined = true;
            AddPostSerialListener(nameof(ManageChangingPlayers));
            Serialize();
        }

        public void ManageChangingPlayers()
        {
            foreach (Player player in Players)
            {
                if (player.HasOwner)
                {
                    if (!player.HasMoney)
                        player.SendToOwner(nameof(Player.Leave));
                    else
                        player.SendToOwner(nameof(Player.JoinFullyFromLateness));
                }
            }
        }

        private void WaitForPlayersDetermined()
        {
            if (NumLateJoiners == 0 && NumPlayers == NumPlayersAfterRound)
            {
                waitingForPlayersDetermined = false;

                AddPostSerialListener(nameof(ReturnAllCards));
                Serialize();
            }
        }

        public void ReturnAllCards()
        {
            waitingForCardsReturned = true;
            Serialize();
            
            GameDeck.ReturnAll();
        }

        private void WaitForCardsReturned()
        {
            if (GameDeck.AllReturned)
            {
                waitingForCardsReturned = false;

                AddPostSerialListener(nameof(DealForRound));
                Serialize();
            }
        }

        public void DealForRound()
        {
            GameDeck.Shuffle();
            DrawCommunityCards();

            foreach (Player player in Players)
            {
                if (player.HasOwner)
                    GameDeck.DrawCardsFor(player.Owner, Hand.Size);
            }

            CalculateAllBestHands();
            HardResetStatuses();

            AddPostSerialListener(nameof(PerformStartOfRoundTasks));
            Serialize();
        }

        public void PerformStartOfRoundTasks()
        {
            GoToNextDealer();

            int smallBlindPlayer = GetPlayerNextTo(curDealerIdx);
            int bigBlindPlayer = GetPlayerNextTo(smallBlindPlayer);

            // If we are using heads up rules (i.e. we have only 2 players), post the blinds differently
            if (UsingHeadsUpRules)
            {
                smallBlindPlayer = curDealerIdx;
                bigBlindPlayer = GetPlayerNextTo(curDealerIdx);
            }

            AddToConsole($"Small blind is P{smallBlindPlayer}, big blind is P{bigBlindPlayer}");

            foreach (Player player in Players)
            {
                if (!player.HasOwner)
                    continue;

                if (player.PlayerNum == smallBlindPlayer)
                    player.SendToOwner(nameof(Player.PerformSmallBlindTasks));
                else if (player.PlayerNum == bigBlindPlayer)
                    player.SendToOwner(nameof(Player.PerformBigBlindTasks));
                else
                    player.SendToOwner(nameof(Player.PerformStartOfRoundTasks));
            }

            waitingForNextRound = true;
            ++curRound;
            Serialize();
        }

        private void WaitForNextRound()
        {
            bool allReady = true;
            int numCurBlinds = 0, numExpectedBlinds = 0;

            int smallBlindPlayer = GetPlayerNextTo(curDealerIdx);
            int bigBlindPlayer = GetPlayerNextTo(smallBlindPlayer);

            // If we are using heads up rules (i.e. we have only 2 players), post the blinds differently
            if (UsingHeadsUpRules)
            {
                smallBlindPlayer = curDealerIdx;
                bigBlindPlayer = GetPlayerNextTo(curDealerIdx);
            }

            numExpectedBlinds += Players[smallBlindPlayer].HasOwner ? 1 : 0;
            numExpectedBlinds += Players[bigBlindPlayer].HasOwner ? 1 : 0;

            foreach (Player player in Players)
            {
                if (!player.HasOwner)
                    continue;

                numCurBlinds += player.GetBet(PreflopStreet) > 0 ? 1 : 0;

                if (player.CurRound != curRound)
                {
                    allReady = false;
                    break;
                }
            }

            // If everyone has confirmed their readiness by matching the round number and there's enough blinds
            // (either the blinds were posted correctly or one or both of them left, in which case just go with
            // whoever is left or worst case have no bets in the pre-flop), then continue
            if (allReady && numCurBlinds >= numExpectedBlinds)
            {
                waitingForNextRound = false;
                Serialize();

                GoToNextStreet();
            }
        }

        public void DrawCommunityCards()
        {
            CommunityCardAnimators[Card.Flop1].RequestAnimation("Default");
            CommunityCardAnimators[Card.Flop2].RequestAnimation("Default");
            CommunityCardAnimators[Card.Flop3].RequestAnimation("Default");
            CommunityCardAnimators[Card.Turn].RequestAnimation("Default");
            CommunityCardAnimators[Card.River].RequestAnimation("Default");

            GameDeck.DrawCardsFor(Owner, CommunityCardSize);
            int[] cardIdxs = GameDeck.GetCardIndices();

            if (cardIdxs == null || cardIdxs.Length != CommunityCardSize)
            {
                // TODO: Some error
                return;
            }

            // Save into named variables for easier coding and syncing

            flop1CardIdx = cardIdxs[Card.Flop1];
            flop2CardIdx = cardIdxs[Card.Flop2];
            flop3CardIdx = cardIdxs[Card.Flop3];
            turnCardIdx  = cardIdxs[Card.Turn];
            riverCardIdx = cardIdxs[Card.River];

            CommunityCardAnimators[Card.Flop1].GetCard();
            CommunityCardAnimators[Card.Flop2].GetCard();
            CommunityCardAnimators[Card.Flop3].GetCard();
            CommunityCardAnimators[Card.Turn].GetCard();
            CommunityCardAnimators[Card.River].GetCard();
        }

        public bool IsCommunityCard(int idx)
            => idx == Flop1CardIdx
            || idx == Flop2CardIdx
            || idx == Flop3CardIdx
            || idx == TurnCardIdx
            || idx == RiverCardIdx;

        //private void CalculateBestHand(Card c1, Card c2, Card c3, Card c4, Card c5, Card unused1, Card unused2, out int bestHand, out int score)
        private void CalculateBestHand(Card c1, Card c2, Card c3, Card c4, Card c5, out int bestHand, out long score, out long kicker)
        {
            // REMARK: Look trust me I hate the look of this code you're about to see too but
            // Udon is way slower than C# which is way slower than C++. We need to calculate the best
            // hands of up to 8 players without discernable lag (i.e. no brute force methods that would
            // cause a freeze). The need for efficiency takes precedence in this case

            // ----------------------------------------------------------------------------------------

            // Keep tracks of the cards in rank order to make determining the best hand easier

            // REMARK: Note that we have 14 values instead of 13. Position #14 is the high ace position,
            // which is the same value as the low ace value such that they both account for high and low
            // straights and straight flushes.

            char[] handData = { '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0' };

            // Keep a count of the number of cards from this hand each suit has (for a possible flush)
            // REMARK: C# does not allow hard casts from bool to int are you KIDDING ME

            int spadesCount =
                  (c1.Suit == Card.Spades ? 1 : 0)
                + (c2.Suit == Card.Spades ? 1 : 0)
                + (c3.Suit == Card.Spades ? 1 : 0)
                + (c4.Suit == Card.Spades ? 1 : 0)
                + (c5.Suit == Card.Spades ? 1 : 0);
            int heartsCount =
                  (c1.Suit == Card.Hearts ? 1 : 0)
                + (c2.Suit == Card.Hearts ? 1 : 0)
                + (c3.Suit == Card.Hearts ? 1 : 0)
                + (c4.Suit == Card.Hearts ? 1 : 0)
                + (c5.Suit == Card.Hearts ? 1 : 0);
            int clubsCount =
                  (c1.Suit == Card.Clubs ? 1 : 0)
                + (c2.Suit == Card.Clubs ? 1 : 0)
                + (c3.Suit == Card.Clubs ? 1 : 0)
                + (c4.Suit == Card.Clubs ? 1 : 0)
                + (c5.Suit == Card.Clubs ? 1 : 0);
            int diamondsCount =
                  (c1.Suit == Card.Diamonds ? 1 : 0)
                + (c2.Suit == Card.Diamonds ? 1 : 0)
                + (c3.Suit == Card.Diamonds ? 1 : 0)
                + (c4.Suit == Card.Diamonds ? 1 : 0)
                + (c5.Suit == Card.Diamonds ? 1 : 0);

            // Keep track of the score of the unused cards to add to the final score (for kickers, i.e. tiebreakers)
            //int unusedScore =
            //      (unused1.Rank == Card.LowAce ? Card.HighAce : unused1.Rank)
            //    + (unused2.Rank == Card.LowAce ? Card.HighAce : unused2.Rank);

            // Organize all the cards by rank in our hand data
            // REMARK: You can't do |= in UdonSharp which is super cringe

            handData[c1.Rank] = (char)(handData[c1.Rank] | (1 << c1.Suit));
            handData[c2.Rank] = (char)(handData[c2.Rank] | (1 << c2.Suit));
            handData[c3.Rank] = (char)(handData[c3.Rank] | (1 << c3.Suit));
            handData[c4.Rank] = (char)(handData[c4.Rank] | (1 << c4.Suit));
            handData[c5.Rank] = (char)(handData[c5.Rank] | (1 << c5.Suit));

            // Keep the ace score consistent between high and low scores
            handData[Card.HighAce] = handData[Card.LowAce];

            // Clear the best hand for the best hand logic
            bestHand = -1;
            // Set the kicker score to be the sum of the cards (assuming high aces) by default
            long[] cardsPow10 = { 1, 1, 1, 1, 1 };
            Card[] cardList = { c1, c2, c3, c4, c5 };

            for (int cardIdx = 0; cardIdx < cardList.Length; ++cardIdx)
            {
                for (int pow = 0; pow < (cardList[cardIdx].Rank == Card.LowAce ? Card.HighAce : cardList[cardIdx].Rank) - 1; ++pow)
                    cardsPow10[cardIdx] *= 10;
            }

            kicker = cardsPow10[0] + cardsPow10[1] + cardsPow10[2] + cardsPow10[3] + cardsPow10[4];

            //kicker =
            //      (long)Mathf.Pow(10, (c1.Rank == Card.LowAce ? Card.HighAce : c1.Rank) - 1)
            //    + (long)Mathf.Pow(10, (c2.Rank == Card.LowAce ? Card.HighAce : c2.Rank) - 1)
            //    + (long)Mathf.Pow(10, (c3.Rank == Card.LowAce ? Card.HighAce : c3.Rank) - 1)
            //    + (long)Mathf.Pow(10, (c4.Rank == Card.LowAce ? Card.HighAce : c4.Rank) - 1)
            //    + (long)Mathf.Pow(10, (c5.Rank == Card.LowAce ? Card.HighAce : c5.Rank) - 1);
                //+ (c2.Rank == Card.LowAce ? Card.HighAce : c2.Rank)
                //+ (c3.Rank == Card.LowAce ? Card.HighAce : c3.Rank)
                //+ (c4.Rank == Card.LowAce ? Card.HighAce : c4.Rank)
                //+ (c5.Rank == Card.LowAce ? Card.HighAce : c5.Rank);
            //+ unusedScore;

            int scoreIfStraight = -1;
            CheckStraights(handData, ref bestHand, ref scoreIfStraight);

            // If we found a royal or straight flush, return that immediately
            // If we found a straight, there still might be a better hand
            if (bestHand != -1 && bestHand != Hand.Straight)
            {
                //score = scoreIfStraight + unusedScore;
                score = scoreIfStraight;
                return;
            }

            // Check the rest of the types of hands

            // For four-of-a-kinds
            int best4ofSame = -1;
            // For full houses and three of a kinds
            int best3ofSame = -1;
            // For full houses, two pairs, and one pairs.
            // The algorithm will detect the best 3 as the best 2 as well, so we need a second best pair
            // for full houses. The reason for two pairs is obvious
            int best2ofSame = -1, secondBest2ofSame = -1;
            // For high cards
            int bestHighCard = -1;

            CheckVariableKinds(handData, ref best4ofSame, ref best3ofSame, ref best2ofSame, ref secondBest2ofSame, ref bestHighCard);

            // Check the rest of the hand ranks from greatest to least playing power

            if (best4ofSame != -1)
            {
                bestHand = Hand.FourOfAKind;
                score = best4ofSame;
            }

            else if (best3ofSame != -1 &&
                ((best2ofSame != -1 && best3ofSame != best2ofSame) ||
                 (secondBest2ofSame != -1 && best3ofSame != secondBest2ofSame)))
            {
                bestHand = Hand.FullHouse;
                score = best3ofSame + best2ofSame + secondBest2ofSame;
            }

            else if (spadesCount == Hand.FinalSize || heartsCount == Hand.FinalSize || clubsCount == Hand.FinalSize || diamondsCount == Hand.FinalSize)
            {
                bestHand = Hand.Flush;
                score = kicker;
            }

            // If we still have a straight after the above hand rank checks,
            // set the hand score as the straight score
            else if (bestHand == Hand.Straight)
                score = scoreIfStraight;// + unusedScore;

            else if (best3ofSame != -1)
            {
                bestHand = Hand.ThreeOfAKind;
                score = best3ofSame;
            }

            else if (best2ofSame != -1)
            {
                bestHand = secondBest2ofSame != -1 ? Hand.TwoPair : Hand.OnePair;
                score = best2ofSame;
            }

            else
            {
                bestHand = Hand.HighCard;
                score = bestHighCard;
            }
        }

        private void CheckStraights(in char[] handData, ref int bestHand, ref int scoreIfStraight)
        {
            // REMARK: The ONLY reason we have to pass the straight hand score in is because
            // of the (steel) wheel hand i.e. the hand is a five-high straight (flush) i.e. 5432A[S]
            // I love edge cases!!!

            // Loop backwards to prioritize higher scoring straights / straight flushes
            // (from high ace to low ace)
            for (int i = handData.Length - Hand.FinalSize; i >= 0; --i)
            {
                // If we have a straight flush of any kind, set our hand as that
                if (handData[i] != 0 && (handData[i] & handData[i + 1] & handData[i + 2] & handData[i + 3] & handData[i + 4]) == handData[i])
                {
                    // Set the straight flush type
                    bestHand = i == handData.Length - Hand.FinalSize ? Hand.RoyalFlush : Hand.StraightFlush;

                    // Accumulate all the ranks as the hand score

                    // REMARK: We can use the position variable to calculate ranks since
                    // it matches them 1-1 in a straight flush
                    // Therefore i + (i + 1) + (i + 2) + (i + 3) + (i + 4) = i * 5 + 10

                    scoreIfStraight = i * 5 + 10;

                    // We have the best hand so go back
                    return;
                }
                // If we haven't found a straight (flush) and we found a straight, set our hand as that
                else if (bestHand == -1 && handData[i] != 0 && handData[i + 1] != 0 && handData[i + 2] != 0 && handData[i + 3] != 0 && handData[i + 4] != 0)
                {
                    bestHand = Hand.Straight;

                    // Accumulate all the ranks as the hand score

                    // REMARK: We can use the position variable to calculate ranks since
                    // it matches them 1-1 in a straight
                    // Therefore i + (i + 1) + (i + 2) + (i + 3) + (i + 4) = i * 5 + 10

                    scoreIfStraight = i * 5 + 10;

                    // Continue to check for a better hand even though we found the best straight
                }
            }
        }

        private int GetRankCount(char cardRep) =>
              ((cardRep & Card.SpadesBit)   != 0 ? 1 : 0)
            + ((cardRep & Card.HeartsBit)   != 0 ? 1 : 0)
            + ((cardRep & Card.ClubsBit)    != 0 ? 1 : 0)
            + ((cardRep & Card.DiamondsBit) != 0 ? 1 : 0);

        private void CheckVariableKinds(in char[] handData, ref int best4, ref int best3, ref int best2_1, ref int best2_2, ref int best1)
        {
            // Loop backwards to prioritize better cards (from high ace to two/deuce)
            // Continue to loop until we found a four of a kind or we found the necessary components
            // for the rest of the hands or until we've checked all the cards
            for (int i = handData.Length - 1; i > 0 && best4 == -1 && (best3 == -1 || best2_1 == -1 || best2_2 == -1 || best1 == -1); --i)
            {
                int rankCount = GetRankCount(handData[i]);

                if (best1 == -1 && rankCount > 0)
                    best1 = i;

                if (rankCount > 1)
                {
                    if (best2_1 == -1)
                        best2_1 = i;
                    else if (best2_2 == -1)
                        best2_2 = i;
                }

                if (best3 == -1 && rankCount > 2)
                    best3 = i;

                if (best4 == -1 && rankCount > 3)
                    best4 = i;
            }
        }

        private void UpdateBestHand(Card c1, Card c2, Card c3, Card c4, Card c5, ref int prevBestHand, ref long prevScore, ref long prevKicker)
        {
            Debug.Log($"Checking hand: {c1.FaceName}, {c2.FaceName}, {c3.FaceName}, {c4.FaceName}, {c5.FaceName}");

            //CalculateBestHand(c1, c2, c3, c4, c5, unused1, unused2, out int curBestHand, out int curScore);
            CalculateBestHand(c1, c2, c3, c4, c5, out int curBestHand, out long curScore, out long curKicker);

            Debug.Log($"Result is hand {curBestHand}, score {curScore}, kicker {curKicker}");

            // If the found hand rank is better, or is the same but with a better score / kicker,
            // use the found hand instead
            if (curBestHand > prevBestHand || (curBestHand == prevBestHand && curScore > prevScore) || (curBestHand == prevBestHand && curScore == prevScore && curKicker > prevKicker))
            {
                prevBestHand = curBestHand;
                prevScore = curScore;
                prevKicker = curKicker;
            }
        }

        private void CalculateAllBestHands()
        {
            Debug.Log("---------- BEGIN " + nameof(CalculateAllBestHands) + " ----------");

            // Clear the old best hands and winners
            for (int i = 0; i < Players.Length; ++i)
            {
                bestFlopHands[i] = -1;
                bestFlopScores[i] = -1;
                bestFlopKickers[i] = -1;
                bestTurnHands[i] = -1;
                bestTurnScores[i] = -1;
                bestTurnKickers[i] = -1;
                bestRiverHands[i] = -1;
                bestRiverScores[i] = -1;
                bestRiverKickers[i] = -1;

                winRankings[i] = Player.InvalidNum;
            }

            firstWinner = Player.InvalidNum;

            Card flop1 = GameDeck.GetCard(flop1CardIdx).GetComponent<Card>();
            Card flop2 = GameDeck.GetCard(flop2CardIdx).GetComponent<Card>();
            Card flop3 = GameDeck.GetCard(flop3CardIdx).GetComponent<Card>();
            Card turn = GameDeck.GetCard(turnCardIdx).GetComponent<Card>();
            Card river = GameDeck.GetCard(riverCardIdx).GetComponent<Card>();

            Debug.Log($"Flop #1: {flop1.FaceName}, Flop #2: {flop2.FaceName}, Flop #3: {flop3.FaceName}");
            Debug.Log($"Turn: {turn.FaceName}, River: {river.FaceName}");

            for (int i = 0; i < Players.Length; ++i)
            {
                Player player = Players[i];

                if (!player.HasOwner)
                    continue;

                Debug.Log($"[P{i}] Player at table? {player.HasOwner}");

                int hole1Idx = -1;
                int hole2Idx = -1;

                int[] cardIndices = GameDeck.GetCardIndicesOf(player.OwnerID);

                if (player.OwnedByLocal) // Is the game master
                {
                    foreach (int idx in cardIndices)
                    {
                        if (IsCommunityCard(idx))
                            continue;
                        else if (hole1Idx == -1)
                            hole1Idx = idx;
                        else if (hole2Idx == -1)
                            hole2Idx = idx;
                        else
                            break;
                    }
                }
                else
                {
                    hole1Idx = cardIndices[0];
                    hole2Idx = cardIndices[1];
                }

                Card hole1 = GameDeck.GetCardOf(player.OwnerID, hole1Idx).GetComponent<Card>();
                Card hole2 = GameDeck.GetCardOf(player.OwnerID, hole2Idx).GetComponent<Card>();

                Debug.Log($"[P{player.PlayerNum}] Hole #1: {hole1.FaceName}, Hole #2: {hole2.FaceName}");

                // Find the best possible hands during the streets where cards are dealt

                // REMARK: Fun fact: by combination (7, 5) there are only 21 ways to choose 5
                // of the 7 given cards to use for a hand

                // Flop hand
                UpdateBestHand(hole1, hole2, flop1, flop2, flop3, ref bestFlopHands[i], ref bestFlopScores[i], ref bestFlopKickers[i]);

                // Turn hands
                UpdateBestHand(hole1, hole2, flop1, flop2, turn, ref bestTurnHands[i], ref bestTurnScores[i], ref bestTurnKickers[i]);
                UpdateBestHand(hole1, hole2, flop1, flop3, turn, ref bestTurnHands[i], ref bestTurnScores[i], ref bestTurnKickers[i]);
                UpdateBestHand(hole1, hole2, flop2, flop3, turn, ref bestTurnHands[i], ref bestTurnScores[i], ref bestTurnKickers[i]);
                UpdateBestHand(hole1, flop1, flop2, flop3, turn, ref bestTurnHands[i], ref bestTurnScores[i], ref bestTurnKickers[i]);
                UpdateBestHand(hole2, flop1, flop2, flop3, turn, ref bestTurnHands[i], ref bestTurnScores[i], ref bestTurnKickers[i]);

                // River hands
                UpdateBestHand(hole1, hole2, flop1, flop2, river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, hole2, flop1, flop3, river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, hole2, flop2, flop3, river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, hole2, flop1, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, hole2, flop2, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, hole2, flop3, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);

                UpdateBestHand(hole1, flop1, flop2, flop3, river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, flop1, flop2, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, flop1, flop3, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole1, flop2, flop3, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);

                UpdateBestHand(hole2, flop1, flop2, flop3, river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole2, flop1, flop2, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole2, flop1, flop3, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);
                UpdateBestHand(hole2, flop2, flop3, turn,  river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);

                UpdateBestHand(flop1, flop2, flop3, turn, river, ref bestRiverHands[i], ref bestRiverScores[i], ref bestRiverKickers[i]);

                // Place the current player in the win rankings

                UpdateWinRankings(i);
            }

            Debug.Log($"Win rankings: {firstWinner}, [{winRankings[0]}, {winRankings[1]}, {winRankings[2]}, {winRankings[3]}, {winRankings[4]}, {winRankings[5]}, {winRankings[6]}, {winRankings[7]}]");

            Debug.Log("---------- END " + nameof(CalculateAllBestHands) + " ----------");
        }

        private void GetBestHandOf(int playerNum, out int winningHand, out long winningScore, out long winningKicker)
        {
            winningHand = bestFlopHands[playerNum];
            winningScore = bestFlopScores[playerNum];
            winningKicker = bestFlopKickers[playerNum];

            int bestTurnHand  = bestTurnHands[playerNum];
            long bestTurnScore = bestTurnScores[playerNum];
            long bestTurnKicker = bestTurnKickers[playerNum];

            if (bestTurnHand > winningHand || (bestTurnHand == winningHand && bestTurnScore > winningScore) || (bestTurnHand == winningHand && bestTurnScore == winningScore && bestTurnKicker > winningKicker))
            {
                winningHand = bestTurnHand;
                winningScore = bestTurnScore;
                winningKicker = bestTurnKicker;
            }

            int bestRiverHand  = bestRiverHands[playerNum];
            long bestRiverScore = bestRiverScores[playerNum];
            long bestRiverKicker = bestRiverKickers[playerNum];

            if (bestRiverHand > winningHand || (bestRiverHand == winningHand && bestRiverScore > winningScore) || (bestRiverHand == winningHand && bestRiverScore == winningScore && bestRiverKicker > winningKicker))
            {
                winningHand = bestRiverHand;
                winningScore = bestRiverScore;
                winningKicker = bestRiverKicker;
            }
        }

        private void UpdateWinRankings(int givenPlayer)
        {
            // If there are no rankings yet, rank ourselves as the highest scoring player
            if (firstWinner == Player.InvalidNum)
            {
                firstWinner = givenPlayer;
                return;
            }

            GetBestHandOf(givenPlayer, out int givenHand, out long givenScore, out long givenKicker);

            int prevPlayer = Player.InvalidNum;
            int curPlayer = firstWinner;

            while (curPlayer != Player.InvalidNum)
            {
                GetBestHandOf(curPlayer, out int curHand, out long curScore, out long curKicker);

                // If we are equal or better than the current player, rank ourselves above them
                if (givenHand > curHand || (givenHand == curHand && givenScore > curScore) || (givenHand == curHand && givenScore == curScore && givenKicker >= curKicker))
                {
                    // If the current player was the current best, rank ourselves as the highest scoring player
                    if (prevPlayer == Player.InvalidNum)
                        firstWinner = givenPlayer;
                    // Otherwise rank ourselves below the previous player
                    else
                        winRankings[prevPlayer] = givenPlayer;

                    // Rank ourselves above the current player
                    winRankings[givenPlayer] = curPlayer;

                    // No need to do anything else, we've found our place for now
                    return;
                }
                else
                {
                    // Move to the player w/ the next best score
                    prevPlayer = curPlayer;
                    curPlayer = winRankings[curPlayer];
                }
            }

            // If we made it to the end of the list, rank ourselves as the lowest scoring player
            if (curPlayer == Player.InvalidNum)
            {
                winRankings[prevPlayer] = givenPlayer;
                winRankings[givenPlayer] = Player.InvalidNum;
            }
        }

        public string GetBestHandName(int playerNum)
        {
            // REMARK: Fuck this function in particular

            if (!GameStarted || Processing || curStreet < PreflopStreet || curStreet >= NumStreets || playerNum < 0 || playerNum >= Players.Length)
                return NoHand;

            Player player = Players[playerNum];

            if (!player.HasOwner || player.LateJoiner)
                return NoHand;

            if (curStreet == FlopStreet)
            {
                int bestHand = bestFlopHands[playerNum];

                if (bestHand < Hand.HighCard || bestHand >= Hand.NumPokerHands)
                    return NoHand;

                return HandRanks[LocalSettings.LanguageIndex][bestHand];
            }
            else if (curStreet == TurnStreet)
            {
                int bestFlopHand = bestFlopHands[playerNum];
                long bestFlopScore = bestFlopScores[playerNum];
                long bestFlopKicker = bestFlopKickers[playerNum];

                int bestTurnHand = bestTurnHands[playerNum];
                long bestTurnScore = bestTurnScores[playerNum];
                long bestTurnKicker = bestTurnKickers[playerNum];

                if (bestFlopHand < Hand.HighCard || bestFlopHand >= Hand.NumPokerHands || bestTurnHand < Hand.HighCard || bestTurnHand >= Hand.NumPokerHands)
                    return NoHand;

                if (bestFlopScore < 0 || bestTurnScore < 0 || bestFlopKicker < 0 || bestTurnKicker < 0)
                    return NoHand;

                if (bestTurnHand > bestFlopHand || (bestTurnHand == bestFlopHand && bestTurnScore > bestFlopScore) || (bestTurnHand == bestFlopHand && bestTurnScore == bestFlopScore && bestTurnKicker > bestFlopKicker))
                    return HandRanks[LocalSettings.LanguageIndex][bestTurnHand];

                return HandRanks[LocalSettings.LanguageIndex][bestFlopHand];

            }
            else if (curStreet >= RiverStreet && curStreet < NumStreets)
            {
                int winningHand = -1;
                long _unused1 = -1, _unused2 = -1;

                GetBestHandOf(playerNum, out winningHand, out _unused1, out _unused2);

                // TODO: Figure out why this code is reached at the end of the game
                if (winningHand < Hand.HighCard || winningHand >= Hand.NumPokerHands)
                    return NoHand;

                return HandRanks[LocalSettings.LanguageIndex][winningHand];
            }

            return NoHand;
        }

        public void OnPlayerChoseOption()
        {
            SetCurStatus(curPlayerIdx, Players[curPlayerIdx].CurStatus);
            Serialize();

            AdvanceFromPlayer();
        }

        public void AdvanceFromPlayer()
        {
            Debug.Log("&(*&(*&*(*&)( ADVANCE FROM PLAYER &(*&^(**&(&(*");

            if (AllButOneFolded)
                EndRound();
            else if (ReadyToAdvanceStreets)
            {
                CalculateSidePots();
                Debug.Log("---------- END " + nameof(CalculateSidePots) + " ----------");

                if (curStreet != ShowdownStreet && OneOrLessActionablePlayers)
                {
                    // ...skip straight to the showdown

                    if (curStreet == PreflopStreet) GoToNextStreet();
                    if (curStreet == FlopStreet) GoToNextStreet();
                    if (curStreet == TurnStreet) GoToNextStreet();
                }

                GoToNextStreet();
            }
            else
                GoToNextPlayer();
        }

        private void HardResetStatuses() => curStatuses = 0;

        private void SoftResetStatuses()
        {
            for (int i = 0; i < Players.Length; ++i)
            {
                if (GetCurStatus(i) == FoldedStatus)
                    continue;

                SetCurStatus(i, NoStatus);
            }
        }

        private void GoToNextStreet()
        {
            processing = false;

            if (curStreet == ShowdownStreet)
            {
                EndRound();
                return;
            }

            ++curStreet;

            AddToConsole($"Current street is {curStreet}");

            curPlayerIdx = curDealerIdx;

            if (curStreet == PreflopStreet)
            {
                SoftResetStatuses();

                // Set the player index to that of the big blind
                
                // In heads up rules (2 players), this is the other player
                if (UsingHeadsUpRules)
                    curPlayerIdx = GetPlayerNextTo(curPlayerIdx);

                // Otherwise it is the player two spots away from the dealer
                else
                    curPlayerIdx = GetPlayerNextTo(GetPlayerNextTo(curPlayerIdx));

                // Then advance to the next actual player
                GoToNextPlayer();
            }
            else
            {
                int lastBetterIdx = LastBetterIndex;

                SoftResetStatuses();

                // Set the player index to be the player before the one who's about to play

                if (curStreet == ShowdownStreet && !AllChecked)
                    curPlayerIdx = GetPlayerPreviousTo(lastBetterIdx);

                else if (UsingHeadsUpRules)
                    curPlayerIdx = curDealerIdx;

                // Then advance to the next actual player
                GoToNextPlayer();
            }

            if (curStreet == FlopStreet)
            {
                CommunityCardAnimators[Card.Flop1].RequestAnimation("Turn");
                CommunityCardAnimators[Card.Flop2].RequestAnimation("Turn");
                CommunityCardAnimators[Card.Flop3].RequestAnimation("Turn");
            }
            else if (curStreet == TurnStreet)
                CommunityCardAnimators[Card.Turn].RequestAnimation("Turn");
            else if (curStreet == RiverStreet)
                CommunityCardAnimators[Card.River].RequestAnimation("Turn");

            Serialize();
        }

        private void GoToNextPlayer()
        {
            int prevPlayerIdx = curPlayerIdx;

            for (int i = 1; i < Players.Length; ++i)
            {
                curPlayerIdx = prevPlayerIdx + i;

                if (curPlayerIdx >= Players.Length)
                    curPlayerIdx -= Players.Length;

                Player player = Players[curPlayerIdx];

                // If we've found a player who's in the game and hasn't folded and either we are in the showdown
                // (where players who haven't folded regardless of the money they have must play) or
                // we are in one of the other streets (where players must have money to play) and we have money,
                // choose this player
                if (player.HasOwner && !player.LateJoiner && GetCurStatus(curPlayerIdx) != FoldedStatus && (curStreet == ShowdownStreet || player.HasMoney))
                    break;
            }

            AddToConsole($"Used to be {prevPlayerIdx}, now {curPlayerIdx}");

            AddPostSerialListener(nameof(PerformStartOfTurnTasks));

            Serialize();
        }

        // REMARK: Doesn't take money into account
        private int GetPlayerPreviousTo(int playerIdx)
        {
            int prevPlayerIdx = playerIdx;

            for (int i = 1; i < Players.Length; ++i)
            {
                prevPlayerIdx = playerIdx - i;

                if (prevPlayerIdx < 0)
                    prevPlayerIdx += Players.Length;

                Player prevPlayer = Players[prevPlayerIdx];

                if (prevPlayer.HasOwner && !prevPlayer.LateJoiner && GetCurStatus(prevPlayerIdx) != FoldedStatus)
                    break;
            }

            return prevPlayerIdx;
        }

        // REMARK: Doesn't take money into account
        private int GetPlayerNextTo(int playerIdx)
        {
            int nextPlayerIdx = playerIdx;

            for (int i = 1; i < Players.Length; ++i)
            {
                nextPlayerIdx = playerIdx + i;

                if (nextPlayerIdx >= Players.Length)
                    nextPlayerIdx -= Players.Length;

                Player nextPlayer = Players[nextPlayerIdx];

                if (nextPlayer.HasOwner && !nextPlayer.LateJoiner && GetCurStatus(nextPlayerIdx) != FoldedStatus)
                    break;
            }

            return nextPlayerIdx;
        }

        public void PerformStartOfTurnTasks()
        {
            Debug.Log($"^^^^^^^^^^^^^^ CUR PLAYER IS P{curPlayerIdx} ^^^^^^^^^^^^^^");
            Players[curPlayerIdx].SendToOwner(nameof(Player.PerformStartOfTurnTasks));
        }

        public void GoToNextDealer()
        {
            int prevDealer = curDealerIdx;

            for (int i = 1; i < Players.Length; ++i)
            {
                curDealerIdx = prevDealer + i;

                if (curDealerIdx >= Players.Length)
                    curDealerIdx -= Players.Length;

                Player player = Players[curDealerIdx];

                if (player.HasOwner && !player.LateJoiner)
                    break;
            }

            Serialize();
        }

        private void CalculateSidePots()
        {
            if (NumPlayersDuringStreet <= 2 || AllChecked)
                return;

            Debug.Log("---------- BEGIN " + nameof(CalculateSidePots) + " ----------");

            // Collect all the money that's been betted this round
            int[] streetBets = new int[Players.Length];

            // Fixed-Rake Algorithm

            // 1) Sort the players from least to most betted this round

            int[] leastToMostBetted = new int[Players.Length];
            int leastBetter = Player.InvalidNum;
            int numBetters = 0;

            for (int i = 0; i < Players.Length; ++i)
                leastToMostBetted[i] = -1;

            foreach (Player player in Players)
            {
                if (!PlayerInStreet(player))
                    continue;

                int playerBet = player.GetBet(curStreet);
                streetBets[player.PlayerNum] = playerBet;

                int prevBetter = Player.InvalidNum;
                int curBetter = leastBetter;

                // Search for the first player who has more money than the current player
                while (curBetter != Player.InvalidNum)
                {
                    int curBettersBet = Players[curBetter].GetBet(curStreet);

                    if (playerBet < curBettersBet)
                    {
                        Debug.Log($"P{player.PlayerNum} has a lesser bet than P{curBetter} (${playerBet} < ${curBettersBet})");
                        break;
                    }
                    else if (playerBet == curBettersBet && player.Bankroll.GetChips() < Players[curBetter].Bankroll.GetChips())
                    {
                        Debug.Log($"P{player.PlayerNum} has a lesser bankroll than P{curBetter} (${player.Bankroll.GetChips()} < ${Players[curBetter].Bankroll.GetChips()})");
                        break;
                    }
                    else
                    {
                        prevBetter = curBetter;
                        curBetter = leastToMostBetted[curBetter];
                    }
                }

                // If we didn't advance past the first least better...
                if (prevBetter == Player.InvalidNum)
                {
                    // ...and there currently isn't a least better, make the current player the least better
                    if (curBetter == Player.InvalidNum)
                        leastBetter = player.PlayerNum;
                    // Otherwise this player has betted less than the previous least better, so push them
                    // to the front of the list
                    else
                    {
                        leastToMostBetted[player.PlayerNum] = leastBetter;
                        leastBetter = player.PlayerNum;
                    }
                }
                // Otherwise insert the player between the previous least better and the next greatest better
                // (or push them the end of the list if the current better is invalid)
                else
                {
                    leastToMostBetted[prevBetter] = player.PlayerNum;
                    leastToMostBetted[player.PlayerNum] = curBetter;
                }

                ++numBetters;
            }

            for (int curBetter = leastBetter; curBetter != Player.InvalidNum; curBetter = leastToMostBetted[curBetter])
            {
                Player player = Players[curBetter];
                Debug.Log($"P{player.PlayerNum} bet ${player.GetBet(curStreet)}, now has ${player.Bankroll.GetChips()}");
            }

            // 2) Create side pot(s) if possible, checking from least to greatest amounts betted

            for (int curBetter = leastBetter; curBetter != Player.InvalidNum; curBetter = leastToMostBetted[curBetter])
            {
                Player player = Players[curBetter];

                // If the player didn't go all in, it is not possible to create anymore side pots
                // So rake the rest of the bets into the current pot
                if (player.Bankroll.GetChips() != 0)
                    return;

                int curBet = streetBets[curBetter];

                // If the previous better had the same amount of chips and created a side pot, that means this player
                // has no chips remaining to create another side pot. There may still be players who can create a side pot,
                // so continue the search
                if (curBet == 0)
                {
                    --numBetters;
                    continue;
                }

                for (int nextBetter = curBetter; nextBetter != Player.InvalidNum; nextBetter = leastToMostBetted[nextBetter])
                {
                    streetBets[nextBetter] -= curBet;

                    // We cannot create anymore side pots, so sweep the rest of the bets into the current pot
                    if (nextBetter != curBetter && streetBets[nextBetter] < 0)
                        return;
                }

                // At this point, we know we can create a side pot

                int curPot = CurPot;

                potLimiters[curPot] = player.PlayerNum;
                potLimits[curPot] = curBet;
                participantsPerPot[curPot] = numBetters;

                --numBetters;
            }
        }

        private int GetPotLimit(int potIdx) => potLimits[potIdx] * participantsPerPot[potIdx];

        public int GetPot(int potIdx)
        {
            if (potIdx < 0 || potIdx >= potLimiters.Length)
                return 0;

            if (potLimiters[potIdx] != Player.InvalidNum)
                return GetPotLimit(potIdx);

            int pot = SumOfAllBets;

            for (int i = potIdx - 1; i >= 0; --i)
                pot -= GetPotLimit(i);

            return pot;
        }

        public int[] GetPotWinners(int potIdx)
        {
            int[] result = new int[MaxTies + 1]; // So we can put the size in front
            result[0] = 0;

            int curWinner = firstWinner;
            GetBestHandOf(curWinner, out int winningHand, out long winningScore, out long winningKicker);

            while (curWinner != Player.InvalidNum)
            {
                if (!ParticipatesInPot(curWinner, potIdx))
                {
                    // If we haven't found a winning score yet, find players of the next best hand
                    if (result[0] == 0)
                        GetBestHandOf(winRankings[curWinner], out winningHand, out winningScore, out winningKicker);
                }
                else
                {
                    GetBestHandOf(curWinner, out int curWinningHand, out long curWinningScore, out long curWinningKicker);

                    // If we have found a winner, add them to the list
                    if (curWinningHand == winningHand && curWinningScore == winningScore && curWinningKicker == winningKicker)
                    {
                        ++result[0];
                        result[result[0]] = curWinner;
                    }
                    // Otherwise stop searching since the rest of the list has lower hands
                    else
                        break;
                }
                curWinner = winRankings[curWinner];
            }

            return result;
        }

        public bool PlayerFolded(int playerNum) => GetCurStatus(playerNum) == FoldedStatus;

        public bool ParticipatesInPot(int playerNum, int potIdx)
        {
            Player player = Players[playerNum];

            if (!player.HasOwner || player.LateJoiner || PlayerFolded(playerNum))
                return false;

            int potCap = potLimiters.Length;

            for (int i = 0; i < potLimiters.Length; ++i)
            {
                // If no one is limiting this pot, we're at the end of the pot list
                if (potLimiters[i] == Player.InvalidNum)
                    break;
                // Otherwise if they are a pot limiter, limit the amount of pots they can participate in
                // to the previous pots plus the pot they're limiting
                else if (potLimiters[i] == playerNum)
                {
                    potCap = i;
                    break;
                }
            }

            // Pot limiters participate in the pot they limit
            return potIdx <= potCap;
        }

        private void ClearPots()
        {
            for (int i = 0; i < potLimiters.Length; ++i)
                potLimiters[i] = Player.InvalidNum;
            // No need to clear the pot limits or participants since they are tied to the pot limiters
        }

        public void ResetRaisePitch() => numRaises = 0;

        public void PlayRisingRaise()
        {
            RaiseJingle.DefaultPitch = Mathf.Pow(1.05946f, numRaises);
            RaiseJingle.Play();
            numRaises += 1;
        }

        public void ResetAllInPitch() => numAllIns = 0;

        public void PlayRisingAllIn()
        {
            AllInJingle.DefaultPitch = Mathf.Pow(1.05946f, numAllIns);
            AllInJingle.Play();
            numAllIns += 1;
        }

        private void EndRound()
        {
            roundEnded = true;

            if (AllButOneFolded)
                WinShortJingle.PlayForAll();
            else
                WinLongJingle.PlayForAll();

            foreach (Player player in Players)
            {
                if (!player.HasOwner || player.LateJoiner)
                    continue;

                player.SendToOwner(nameof(Player.TakeRoundResults));
            }

            if (AllButOneFolded)
                roundEndTimer = ShortRoundEndTime;
            else
                roundEndTimer = LongRoundEndTime;

            Serialize();
        }

        private void OnRoundEndedTimeout()
        {
            if (EnoughPlayersInGame(NumPlayersAfterRound))
            {
                foreach (Player player in Players)
                    player.SendToOwner(nameof(Player.PerformEndOfRoundTasks));

                GoToNextRound();
            }
            else
                EndGame();
        }

        public void EndGame()
        {
            // TODO: More satisfying end

            // Get a list of winners
            prevWinners = "";

            foreach (Player player in Players)
            {
                if (!player.HasOwner || player.LateJoiner || !player.HasMoney)
                    continue;

                prevWinners += player.Owner.displayName + ", ";
            }

            if (prevWinners.Length > 2)
                prevWinners = prevWinners.Remove(prevWinners.Length - 2);

            ResetTable();
        }

        private bool PlayerInStreet(Player player) => player.HasOwner && !player.LateJoiner
                        && GetCurStatus(player.PlayerNum) != FoldedStatus
                        && (player.HasMoney || player.StreetOfLastBet == curStreet);

        private bool EnoughPlayersInGame(int numPlayers) => numPlayers > 1;

        public void SetMidgameJoining(bool on)
        {
            midgameJoining = on;
            Serialize();
        }

        public int GetVotes(int index)
        {
            if (!Players[index].HasOwner)
                return 0;

            int votes = 0;

            foreach (Player player in Players)
                votes += player.ModerationPanel.GetVote(index) ? 1 : 0;

            return votes;
        }

        public void UpdateModeration()
        {
            Debug.Log("&&&&&&&&& Update Moderation &&&&&&&&&&");

            int[] votesPerPlayer = new int[Players.Length];
            int curVoteMajority = VoteMajority;

            Debug.Log($"Votes needed: {curVoteMajority}");

            for (int i = 0; i < votesPerPlayer.Length; ++i)
                votesPerPlayer[i] = 0;

            foreach (Player player in Players)
            {
                if (!player.HasOwner)
                    continue;

                for (int i = 0; i < Players.Length; ++i)
                    votesPerPlayer[i] += player.ModerationPanel.GetVote(i) ? 1 : 0;
            }

            Debug.Log($"Votes cast: [{votesPerPlayer[0]}, {votesPerPlayer[1]}, {votesPerPlayer[2]}, {votesPerPlayer[3]}, {votesPerPlayer[4]}, {votesPerPlayer[5]}, {votesPerPlayer[6]}, {votesPerPlayer[7]}]");

            for (int i = 0; i < Players.Length; ++i)
            {
                Player player = Players[i];

                if (!player.HasOwner)
                    continue;

                if (votesPerPlayer[i] >= curVoteMajority)
                    player.SendToOwner(nameof(Player.Leave));
            }

            SendToAll(nameof(Deserialize));
        }

        public void _AddTranslatable(Translatable translatable)
        {
            int size = translatables == null ? 0 : translatables.Length;
            Translatable[] updated = new Translatable[size + 1];

            if (size > 0)
            {
                for (int i = 0; i < size; ++i)
                    updated[i] = translatables[i];
            }

            updated[size] = translatable;
            translatables = updated;
        }

        public bool _CheckedAtShowdown(int playerIdx) => CurStreet >= ShowdownStreet && GetCurStatus(playerIdx) == CheckedStatus;
    }
}
