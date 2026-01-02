// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDK3.UdonNetworkCalling;  

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Player : Benscript
    {
        public const string PlayerEmoji = "🎮";
        public const string GameMasterEmoji = "👑";
        public const string DealerButton = "<sprite=0>";
        public const string HandEmoji = "🖐";
        public const string FoldedSprite = "<sprite=4>", RevealedSprite = "<sprite=5>";
        public const string ShowdownSubtitleSize = "<size=26>";

        private readonly string[] JoinWithNoGameMasterStrings =
        {
              "Own and Join"
            , "<size=12><line-height=70%>Créer sa propre partie et la rejoindre"
            , "Poseer y participar"
            , "Besitzen und beitreten"
            , "所有と参加"
            , "방장으로 게임 입장하기"
            , "Omista ja Liity"
        };
        private readonly string[] JoinWithGameMasterStrings =
        {
              "Join"
            , "Rejoindre"
            , "Participar"
            , "Beitreten"
            , "参加"
            , "입장하기"
            , "Liity"
        };

        private readonly string[] FoldInsteadStrings =
        {
              "🏳️Fold Instead"
            , "🏳️Se coucher à la place"
            , "🏳️Abandonar en cambio"
            , "🏳️Stattdessen aussteigen"
            , "🏳️代わりにフォールドすること"
            , "🏳️대신 폴드"
            , "🏳️Kippaa Sen Sijaan"
        };
        private readonly string[] RevealInsteadStrings =
        {
              "🃏Reveal Instead"
            , "🃏Révéler à la place"
            , "🃏Presentar en cambio"
            , "🃏Stattdessen präsentieren"
            , "🃏代わりに提示すること"
            , "🃏대신 공개하기"
            , "🃏Näytä Sen Sijaan"
        };

        private readonly string[] MainPotStrings =
        {
              "Main Pot"
            , "Pot principal"
            , "Bote principal"
            , "Hauptpot"
            , "メインポット"
            , "메인 팟"
            , "Pääpotti"
        };
        private readonly string[] SidePotNoMainStrings =
        {
              "Side Pots: "
            , "Pots parallèles : "
            , "Botes laterales: "
            , "Side Pots: "
            , "サイドポット："
            , "사이드 팟: "
            , "Sivupotit: "
        };
        private readonly string[] SidePotWithMainStrings =
        {
              "; Side Pots: "
            , " ; Pots parallèles : "
            , "; Botes laterales: "
            , "; Side Pots: "
            , "；サイドポット："
            , "; 사이드 팟: "
            , "; Sivupotit: "
        };

        private readonly string[] WinByDefaultStrings =
        {
              "By default"
            , "Par défaut"
            , "Por defecto"
            , "Standardmäßig"
            , "デフォルト"
            , "부전승으로"
            , "Oletuksena"
        };

        private readonly string[] HoleCardStrings =
        {
              "Hole Cards"
            , "Cartes privées"
            , "Hole Cards"
            , "Hand-Karten"
            , "屋内カード"
            , "홀 카드"
            , "Pimeät Kortit"
        };

        public const int InvalidNum = -1;

        public const float NumpadNextToCheckX = 50f;
        public const float CheckNextToNumpadX = -10f;

        public const float RevealChoiceTime = 12f; // 7
        public const float ForcedRevealTime = 12f; // 5
        public const float AutoAdvanceCoyoteTime = 2f;

        [Header("Game Variables")]
        public GameManager Manager;

        [Header("Player Parts (Must be relatives)")]
        public Hand Hand;
        public GameObject AddtActionsCanvas;
        public Bankroll Bankroll;
        public AdditionalBet AddtBet;
        public FidgetBet FidgetBet;
        public BetPile BetPile;
        public GameObject BetCanvas;
        public GameObject JoinCanvas;
        public LocalSettingsPanel LocalSettingsPanel;
        public GameObject MasterSettingsCanvas;
        public GameObject HandNameCanvas;
        public GameObject MenuButtonsCanvas;
        public GameObject ActionHints;
        public GameObject ShowdownCanvas;
        public PlayerInfoDisplay PlayerInfoDisplay;
        public RevealedHandInfoDisplay RevealedHandInfoDisplay;
        public GameObject HandRankingsCanvas;
        public SyncedEventTimer ShowdownTimer;
        public GameObject WinnerCanvas;

        [Header("Player Parts (Must not be relatives)")]
        public Button MasterSettingsButton;
        public Button StartGameButton;
        public Button CheckButton, ToggleRevealButton, UseNumpadButton;
        public Button JoinButton, LeaveGameButton, ResetTableButton;
        public Toggle MidgameJoiningToggle;
        public ModerationPanel ModerationPanel;
        public GameObject DefaultShowdownParts, ShowdownPartsJP;
        public SFXPlayer TickTockSFX;

        [Header("Player Signifiers")]
        public TMPro.TextMeshProUGUI JoinText;
        public TMPro.TextMeshProUGUI HandNameText;
        public TMPro.TextMeshProUGUI ShowdownText, ShowdownTextJP;
        public TMPro.TextMeshProUGUI ToggleRevealText;
        public TMPro.TextMeshProUGUI WinnerText;
        public TMPro.TextMeshProUGUI LeaveGameCODAText;

        [Header("Player Effects")]
        public SFXPlayer BetSFX;
        public LayeredEffectPlayer FoldFX, CheckFX, CallFX, RaiseFX, AllInFX, RevealFX, WinnerFX;

        [Header("Player Data")]
        public int PlayerNum = 0;

        [UdonSynced]
        private byte curStatus = GameManager.NoStatus;
        [UdonSynced]
        private int[] bets;
        [UdonSynced]
        private int curRound = 0;
        [UdonSynced]
        private bool lateJoiner = false;
        [UdonSynced]
        private bool winByDefault = false;
        [UdonSynced]
        bool mainPotWon = false;
        [UdonSynced]
        int[] sidePotsWon;
        [UdonSynced]
        int numPotsWon = 0;

        private bool revealChosen = true;

        public bool HasMoney => Bankroll.GetChips() > 0;
        public bool LocalsTurn => Manager.GameStarted && !Manager.RoundEnded && Manager.CurPlayerIndex == PlayerNum;
        public bool CanCheck
        {
            get
            {
                int curGreatestBet = Manager.CurGreatestBet;
                return curGreatestBet == 0 || (Manager.CurStreet > GameManager.InvalidStreet && curGreatestBet == bets[Manager.CurStreet]);
            }
        }

        public byte CurStatus => curStatus;
        public bool DecisionMade => curStatus != GameManager.NoStatus;

        public int CurRound => curRound;

        public bool LateJoiner => lateJoiner;

        public int LastBet
        {
            get
            {
                int lastBet = 0;

                for (int i = bets.Length - 1; i >= 0; --i)
                {
                    int curBet = bets[i];

                    if (curBet != 0)
                    {
                        lastBet = curBet;
                        break;
                    }
                }

                return lastBet;
            }
        }

        public int StreetOfLastBet
        {
            get
            {
                int lastStreet = 0;

                for (int i = bets.Length - 1; i >= 0; --i)
                {
                    int curBet = bets[i];

                    if (curBet != 0)
                    {
                        lastStreet = i;
                        break;
                    }
                }

                return lastStreet;
            }
        }

        public int SumOfAllBets
        {
            get
            {
                int sumOfAllBets = 0;

                foreach (int bet in bets)
                    sumOfAllBets += bet;

                return sumOfAllBets;
            }
        }

        public bool IsDealer => Manager.CurDealerIndex == PlayerNum;

        public string HandNameDisplay => HandEmoji + " " + Manager.GetBestHandName(PlayerNum);

        public bool HasBets
        {
            get
            {
                for (int i = 0; i < bets.Length; ++i)
                {
                    if (bets[i] != 0)
                        return true;
                }
                return false;
            }
        }
        public bool ForcedReveal => Manager.CurStreet == GameManager.ShowdownStreet && (!HasMoney || Manager.OneOrLessActionablePlayers);

        public bool ActionsAllowed =>
            Manager.GameStarted
            && !Manager.Processing
            && OwnedByLocal
            && LocalsTurn
            && !DecisionMade;

        public override void Start()
        {
            bets = new int[GameManager.NumStreets];
            sidePotsWon = new int[Manager.Players.Length];

            LocalSettingsPanel.SFX.Volume = LocalSettingsPanel.Settings.SFXVolume;

            base.Start();
        }

        public override void Deserialize()
        {
            if (OwnedByLocal)
            {
                if (Manager.GameStarted)
                    GoToPlayerState();
                else
                    GoToTakenSlotState();
            }
            else if ((!Manager.GameStarted || Manager.MidgameJoining) && OwnerID == InvalidPlayerID && !Manager.PlayerHasJoined(LocalID))
                GoToAvailableSlotState();
            else
                GoToSpectatorState();

            ModerationPanel.Deserialize();
            LocalSettingsPanel.SFX.Volume = LocalSettingsPanel.Settings.SFXVolume;

            base.Deserialize();
        }

        private void GoToAvailableSlotState()
        {
            if (Manager.OwnerID == InvalidPlayerID)
                JoinText.text = JoinWithNoGameMasterStrings[LocalSettingsPanel.Settings.LanguageIndex];
            else
                JoinText.text = JoinWithGameMasterStrings[LocalSettingsPanel.Settings.LanguageIndex];

            JoinButton.interactable = !Manager.Processing;

            SetRelativesActive(false);
            JoinCanvas.SetActive(true);
        }

        private void GoToTakenSlotState()
        {
            bool localSettingsOpen = LocalSettingsPanel.gameObject.activeSelf;
            bool masterSettingsOpen = Manager.OwnedByLocal && MasterSettingsCanvas.activeSelf;
            bool handRankingsOpen = HandRankingsCanvas.activeSelf;
            bool menuOpen = localSettingsOpen || masterSettingsOpen || handRankingsOpen;

            SetRelativesActive(false);

            Debug.Log($"P{PlayerNum} owns the manager?: {Manager.OwnedByLocal}");

            MasterSettingsButton.interactable = Manager.OwnedByLocal;
            StartGameButton.interactable = !Manager.GameStarted && Manager.HasEnoughPlayers;
            MidgameJoiningToggle.interactable = !Manager.GameStarted;

            LocalSettingsPanel.gameObject.SetActive(localSettingsOpen);
            MasterSettingsCanvas.SetActive(masterSettingsOpen);
            HandRankingsCanvas.SetActive(handRankingsOpen);
            MenuButtonsCanvas.SetActive(!menuOpen);

            LocalSettingsPanel.RespawnCardsButton.interactable = false;
            LocalSettingsPanel._UpdateModerationAvailability();

            LeaveGameButton.interactable = !Manager.Processing;

            LeaveGameCODAText.text = BuildLeaveGameCODA();
        }

        private void GoToPlayerState()
        {
            // Enable the settings and disable all unnecessary objects preemptively
            GoToTakenSlotState();

            // Check button adjustment

            bool enableChecking = Manager.CurStreet != GameManager.ShowdownStreet && CanCheck;

            CheckButton.gameObject.SetActive(enableChecking);

            Vector3 checkBtnPos = CheckButton.transform.localPosition;
            checkBtnPos.x = LocalSettingsPanel.Settings.InputIndex == LocalSettings.SliderInput ? CheckNextToNumpadX : 0f;

            CheckButton.transform.localPosition = checkBtnPos;

            // ---

            // Numpad button adjustment

            UseNumpadButton.gameObject.SetActive(LocalSettingsPanel.Settings.InputIndex == LocalSettings.SliderInput && Manager.CurStreet != GameManager.ShowdownStreet && Bankroll.BetSlider.maxValue != Bankroll.CallAmount);

            Vector3 numpadBtnPos = UseNumpadButton.transform.localPosition;
            numpadBtnPos.x = enableChecking ? NumpadNextToCheckX : 0f;

            UseNumpadButton.transform.localPosition = numpadBtnPos;

            // ---

            ToggleRevealButton.gameObject.SetActive(Manager.CurStreet == GameManager.ShowdownStreet && !ForcedReveal);

            if (ToggleRevealButton.gameObject.activeSelf)
                ToggleRevealText.text = revealChosen ? FoldInsteadStrings[LocalSettingsPanel.Settings.LanguageIndex] : RevealInsteadStrings[LocalSettingsPanel.Settings.LanguageIndex];

            HandNameText.text = HandNameDisplay;

            Hand.gameObject.SetActive(true);
            Bankroll.gameObject.SetActive(true);
            AddtBet.gameObject.SetActive(true);
            FidgetBet.gameObject.SetActive(true);
            BetPile.gameObject.SetActive(true);

            ActionHints.SetActive(!Manager.Processing && LocalsTurn && !DecisionMade && LocalSettingsPanel.Settings.ActionHintsEnabled);
            HandNameCanvas.SetActive(LocalSettingsPanel.Settings.HandNameEnabled);

            //AddtActionsCanvas.SetActive(!Manager.Processing && LocalsTurn && !AddtBet.Pickup.IsHeld && !DecisionMade);

            ShowdownCanvas.SetActive(!Manager.Processing && LocalsTurn && !DecisionMade && Manager.CurStreet == GameManager.ShowdownStreet);

            if (ShowdownCanvas.activeSelf)
            {
                bool usingJP = LocalSettingsPanel.Settings.LanguageIndex == Translatable.LangJP;
                bool revealing = ForcedReveal || revealChosen;

                DefaultShowdownParts.SetActive(!usingJP);
                ShowdownPartsJP.SetActive(usingJP);

                if (usingJP)
                    ShowdownTextJP.text = BuildShowdownStatus(revealing);
                else
                    ShowdownText.text = BuildShowdownStatus(revealing);

                ShowdownTimer.gameObject.SetActive(true);
            }

            LocalSettingsPanel.RespawnCardsButton.interactable = true;

            PlayerInfoDisplay.gameObject.SetActive(true);
            RevealedHandInfoDisplay.gameObject.SetActive(true);

            WinnerCanvas.SetActive(Manager.RoundEnded && (winByDefault || numPotsWon > 0));
            if (WinnerCanvas.activeSelf)
                WinnerText.text = BuildWinString();

            Hand.Pickup.InteractionText = HoleCardStrings[LocalSettingsPanel.Settings.LanguageIndex];
        }

        private void GoToSpectatorState()
        {
            SetRelativesActive(false);

            if (OwnerID != InvalidPlayerID && Manager.GameStarted)
            {
                Hand.gameObject.SetActive(true);
                Bankroll.gameObject.SetActive(true);
                AddtBet.gameObject.SetActive(true);
                FidgetBet.gameObject.SetActive(true);
                BetPile.gameObject.SetActive(true);

                WinnerCanvas.SetActive(Manager.RoundEnded && (winByDefault || numPotsWon > 0));
                if (WinnerCanvas.activeSelf)
                    WinnerText.text = BuildWinString();

                PlayerInfoDisplay.gameObject.SetActive(true);
                RevealedHandInfoDisplay.gameObject.SetActive(true);
            }
        }

        public void Join()
        {
            OwnByLocal();

            curRound = Manager.CurRound;

            if (Manager.GameStarted)
            {
                lateJoiner = true;

                PerformStartOfGameTasks();
                PerformEndOfRoundTasks();
            }

            Serialize();

            // REMARK: LocalSettingsPanel doesn't inherit from Benscript so this has to be done manually
            LocalSettingsPanel.SFX.Volume = LocalSettingsPanel.Settings.SFXVolume;

            // REMARK: Have to exclude this from auto-ownership since it needs to be always on
            ModerationPanel.OwnByLocal();

            // FIX: Only claim GameManager if there is no owner yet to prevent ownership contention
            //if (Manager.OwnerID == InvalidPlayerID)
            if (Manager.OwnerID == InvalidPlayerID || !Manager.OwnerAtTable || Networking.GetOwner(Manager.gameObject) == LocalPlayer)
                Manager.OwnByLocal();

            AddPostSerialListener(nameof(InformManagerOfJoin));
            AddPostSerialListener(nameof(InformOtherManagersOfChange));

            foreach (Player player in Manager.Players)
            {
                if (player == this)
                    continue;
                player.GoToSpectatorState();
            }

            foreach (OutsiderLocalSettings panel in Manager.OutsiderLocalSettingsPanels)
                panel._DisableOutsiderSettings();
        }

        public void InformManagerOfJoin() => Manager.SendToOwner(nameof(GameManager.UpdateJoinedPlayers));

        public void Leave()
        {
            Disown();

            // REMARK: Have to exclude this from auto-ownership since it needs to be always on
            ModerationPanel.SendToOwner(nameof(Disown));

            Manager.SendToOwner(nameof(GameManager.UpdateJoinedPlayers));

            Debug.Log($"************* Called before P{PlayerNum} left *************");
            AddPostSerialListener(nameof(OnLeftTable));
            Serialize();

            foreach (OutsiderLocalSettings panel in Manager.OutsiderLocalSettingsPanels)
                panel._EnableOutsiderSettings();

            // In case it was left on due to leaving right before the Showdown turn ended
            TickTockSFX.Stop();

            AddPostSerialListener(nameof(InformOtherManagersOfChange));
        }

        public void OnLeftTable()
        {
            Debug.Log($"************* Called after P{PlayerNum} left *************");
            if (Manager.GameStarted)
            {
                // If this player is the dealer, make the next player the dealer
                if (Manager.CurDealerIndex == PlayerNum)
                    Manager.SendToOwner(nameof(GameManager.GoToNextDealer));

                // If there are still enough people playing, continue like normal
                if (Manager.HasEnoughPlayers)
                {
                    // If it happened to be that player's turn, advance to the next player
                    if (!Manager.Processing && !Manager.RoundEnded && (LocalsTurn || Manager.OneOrLessActionablePlayers))
                        Manager.SendToOwner(nameof(GameManager.AdvanceFromPlayer));
                }
                // Otherwise shutdown the game
                else
                    Manager.SendToOwner(nameof(GameManager.EndGame));
            }
            else if (Networking.IsOwner(Manager.gameObject) && Manager.OwnerID != InvalidPlayerID)
            {
                Debug.Log("Trying to disown manager after leave");
                Manager.SendToOwner(nameof(Disown));
            }
        }

        public void InformOtherManagersOfChange() => Manager.OnPlayerChangedTable();

        public override void OnOwnershipForceTransferred()
        {
            Debug.Log("Belongs to me but not for long");
            Leave();
        }

        public void _OpenLocalSettings()
        {
            LocalSettingsPanel.MatchSettings();
            LocalSettingsPanel.gameObject.SetActive(true);
            MenuButtonsCanvas.SetActive(false);
        }

        public void _CloseLocalSettings()
        {
            LocalSettingsPanel.gameObject.SetActive(false);
            MenuButtonsCanvas.SetActive(true);
        }

        public void _OpenMasterSettings()
        {
            if (!Manager.OwnedByLocal)
                return;

            MidgameJoiningToggle.SetIsOnWithoutNotify(Manager.MidgameJoining);

            MasterSettingsCanvas.SetActive(true);
            MenuButtonsCanvas.SetActive(false);
        }

        public void _CloseMasterSettings()
        {
            if (!Manager.OwnedByLocal)
                return;

            MasterSettingsCanvas.SetActive(false);
            MenuButtonsCanvas.SetActive(true);
        }

        public void _SetMidgameJoining()
        {
            if (!Manager.OwnedByLocal)
                return;

            Manager.SetMidgameJoining(MidgameJoiningToggle.isOn);
        }

        public void _StartGameAsMaster()
        {
            if (!Manager.OwnedByLocal)
                return;

            _CloseMasterSettings();
            Manager.StartGame();
        }

        public void _RestartGameAsMaster()
        {
            if (!Manager.OwnedByLocal)
                return;
        }

        public void _ResetTableAsMaster()
        {
            if (!Manager.OwnedByLocal)
                return;

            Manager.ResetTable();
        }

        public void _OpenHandRankings()
        {
            HandRankingsCanvas.SetActive(true);
            MenuButtonsCanvas.SetActive(false);
        }

        public void _CloseHandRankings()
        {
            HandRankingsCanvas.SetActive(false);
            MenuButtonsCanvas.SetActive(true);
        }

        public void _ToggleReveal()
        {
            revealChosen = !revealChosen;
            Deserialize();
        }

        public int GetBet(int street) => bets[street];

        public void PerformStartOfGameTasks()
        {
            Bankroll.SetInitialRoll();
            Serialize();
            Manager.SendToOwner(nameof(GameManager.PerformStartOfGameTasks2));
        }

        public void PerformEndOfRoundTasks()
        {
            ClearBets();
            RefreshCards();
            BetPile.ResetBet();
            AddtBet.Respawn();
            FidgetBet.Respawn();
            RevealedHandInfoDisplay.Display.SetActive(false);

            winByDefault = false;
            mainPotWon = false;
            numPotsWon = 0;
            
            Serialize();
        }

        public void PerformStartOfRoundTasks()
        {
            PerformEndOfRoundTasks();

            ++curRound;
            revealChosen = true;
            Serialize();
        }

        public void PerformStartOfRoundTasksCallback()
        {
            PerformStartOfRoundTasks();
            Serialize();
            Manager.SendToOwner(nameof(GameManager.PerformStartOfRoundTasks2));
        }

        public void PerformSmallBlindTasks()
        {
            PerformStartOfRoundTasks();
            ForceSmallBlind();
            Serialize();
            Manager.SendToOwner(nameof(GameManager.PerformStartOfRoundTasks2));
        }

        public void PerformBigBlindTasks()
        {
            Debug.Log("BBBBBBBBBBBBBBBBBBBBBB HERE BBBBBBBBBBBBBBBBBBBBBBBB");
            PerformStartOfRoundTasks();
            ForceBigBlind();
            Serialize();
            Manager.SendToOwner(nameof(GameManager.PerformStartOfRoundTasks2));
        }

        public void PerformStartOfTurnTasks()
        {
            Debug.Log($"$$$$$$$$$$$$$$$$$$$$ P{PlayerNum} RARIN TO GO (Should be P{Manager.CurPlayerIndex}) $$$$$$$$$$$$$$$$$$$$$$$");

            Bankroll.SetMinimumBetFromGame();
            ClearDecision();
            Manager.ActionArea.Deserialize();
            Manager.SendToAll(nameof(Deserialize));
            Manager._DeserializeLocally();

            if (Manager.CurStreet == GameManager.ShowdownStreet)
            {
                ShowdownTimer.TimeInSeconds = ForcedReveal ? ForcedRevealTime : RevealChoiceTime;

                if (!TickTockSFX.gameObject.GetComponent<AudioSource>().isPlaying)
                {
                    ShowdownTimer.StartTimer();
                    TickTockSFX.Play();
                    Manager.TurnJingle.Play();
                }
            }

            if (!Manager.OwnerAtTable)
                Manager.SendToOwner(nameof(Disown));

            if (Manager.CurStreet != GameManager.ShowdownStreet && !Manager.TurnJingle.gameObject.GetComponent<AudioSource>().isPlaying)
                Manager.TurnJingle.Play();
        }

        private void RefreshCards()
        {
            Hand.ResetCards();
            Hand.Respawn();
            Hand.Deserialize();
        }

        public void RespawnCards() => Hand.Respawn();

        private void ClearBets()
        {
            for (int i = 0; i < bets.Length; ++i)
                bets[i] = 0;
        }

        public void OnCardActionChosen()
        {
            Manager.AddToConsole("Trying to switch turns from cards");
            ShowdownTimer.CancelTimer();
            TickTockSFX.Stop();

            byte statusToSend;
            if (Manager.CurStreet >= GameManager.ShowdownStreet && (ForcedReveal || revealChosen))
                statusToSend = GameManager.CheckedStatus;
            else
                statusToSend = GameManager.FoldedStatus;

            Manager.AddToConsole($"Changed curStatus to {statusToSend} for player {PlayerNum}");

            // Play effects locally - they don't need to be synchronized
            if (statusToSend == GameManager.CheckedStatus)
            {
                RevealFX.PlayForAll();
                Manager.RevealJingle.PlayForAll();
            }
            else
            {
                FoldFX.PlayForAll();
                Manager.FoldJingle.PlayForAll();
            }

            Manager.SendToAll(nameof(GameManager.ResetRaisePitch));
            Manager.SendToAll(nameof(GameManager.ResetAllInPitch));

            // FIXED: Pass only the critical status data
            curStatus = statusToSend;
            var actionData = new DataDictionary();
            actionData.Add("status", statusToSend);
            AddPostSerialListenerWithParam(nameof(AdvanceGameWithStatus), actionData);
            Serialize();
        }

        public void OnBetActionChosen()
        {
            Manager.AddToConsole("Trying to switch turns from bet");

            // FIXED: Only send critical status data
            byte statusToSend = GameManager.BettedStatus;
            curStatus = statusToSend;
            int betAmount = AddtBet.GetChips();
            int currentStreet = Manager.CurStreet;

            Manager.AddToConsole($"Changed curStatus to {statusToSend} for player {PlayerNum}");

            bets[currentStreet] += betAmount;
            BetPile.AddChips(betAmount);
            BetSFX.PlayForAll();

            if (Bankroll.GetChips() <= 0)
            {
                AllInFX.PlayForAll();
                Manager.SendToAll(nameof(GameManager.PlayRisingAllIn));
                Manager.SendToAll(nameof(GameManager.ResetRaisePitch));
            }
            else if (bets[currentStreet] > Manager.CurGreatestBet)
            {
                RaiseFX.PlayForAll();
                Manager.SendToAll(nameof(GameManager.PlayRisingRaise));
                Manager.SendToAll(nameof(GameManager.ResetAllInPitch));
            }
            else
            {
                CallFX.PlayForAll();
                Manager.CallJingle.PlayForAll();
                Manager.SendToAll(nameof(GameManager.ResetRaisePitch));
                Manager.SendToAll(nameof(GameManager.ResetAllInPitch));
            }

            // FIXED: Pass only critical data (status, bet amount, street)
            var betData = new DataDictionary();
            betData.Add("status", statusToSend);
            betData.Add("betAmount", bets[currentStreet]);
            betData.Add("street", currentStreet);
            AddPostSerialListenerWithParam(nameof(AdvanceGameWithBetData), betData);
            Serialize();
        }

        public void OnCheckActionChosen()
        {
            if (!CanCheck)
                return;

            Manager.AddToConsole("Trying to switch turns from check");

            // FIXED: Only send critical status data
            byte statusToSend = GameManager.CheckedStatus;
            int currentStreet = Manager.CurStreet;
            curStatus = statusToSend;

            Manager.AddToConsole($"Changed curStatus to {statusToSend} for player {PlayerNum}");

            CheckFX.PlayForAll();
            Manager.CheckJingle.PlayForAll();
            Manager.SendToAll(nameof(GameManager.ResetRaisePitch));
            Manager.SendToAll(nameof(GameManager.ResetAllInPitch));

            // FIXED: Pass only critical data (status, street)
            var checkData = new DataDictionary();
            checkData.Add("status", statusToSend);
            checkData.Add("street", currentStreet);
            AddPostSerialListenerWithParam(nameof(AdvanceGameWithCheckData), checkData);
            Serialize();
        }

        // EVENT: Called by GameManager to reset local status
        public void ResetStatus()
        {
            Debug.Log($"Player {PlayerNum} resetting status from {curStatus} to {GameManager.NoStatus}");
            curStatus = GameManager.NoStatus;
            Serialize();
            
            // Notify GameManager that we've reset
            Manager.SendToOwner(nameof(GameManager.OnPlayerStatusReset));
        }

        // EVENT: Called by GameManager to notify player that their cards are now playable
        public void OnCardsPlayable()
        {
            Debug.Log($"Player {PlayerNum} received OnCardsPlayable event - cards should now be interactable");
            // This event is called after processing is set to false
            // The Hand.Deserialize() method will handle the actual interactability change
            // No need to manually set anything here - just log for debugging
        }

        // FIXED: New method to handle card action with passed status
        [NetworkCallable]
        public void AdvanceGameWithStatus(string json)
        {
            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                // Deserialization succeeded! Let's figure out what we've got.
                if (result.TokenType == TokenType.DataDictionary)
                {
                    Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                    // Use the passed status instead of accessing local field
                    double statusDouble = result.DataDictionary["status"].Double;
                    curStatus = (byte)statusDouble;
                    Manager.SendToOwnerWithParam(nameof(GameManager.OnPlayerChoseOptionWithStatus), json);
                }
                else 
                {
                    Debug.LogError($"Unexpected result when deserializing json {json}");
                }
            } else {
                // Deserialization failed. Let's see what the error was.
                Debug.LogError($"Failed to Deserialize json {json} - {result.ToString()}");
            }

        }

        // FIXED: New method to handle bet action with passed data
        [NetworkCallable]
        public void AdvanceGameWithBetData(string json)
        {

            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                // Deserialization succeeded! Let's figure out what we've got.
                if (result.TokenType == TokenType.DataDictionary)
                {
                    Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                    // Use the passed status instead of accessing local field
                    double statusDouble = result.DataDictionary["status"].Double;
                    double betAmountDouble = result.DataDictionary["betAmount"].Double;
                    double streetDouble = result.DataDictionary["street"].Double;

                    int street = (int)streetDouble;

                    // Use the passed status instead of accessing local field
                    curStatus = (byte)statusDouble;
                    bets[street] = (int)betAmountDouble; // This was already done locally, but ensure consistency
                    
                    Manager.SendToOwnerWithParam(nameof(GameManager.OnPlayerChoseOptionWithStatus), json);
                }
                else 
                {
                    Debug.LogError($"Unexpected result when deserializing json {json}");
                }
            } else {
                // Deserialization failed. Let's see what the error was.
                Debug.LogError($"Failed to Deserialize json {json} - {result.ToString()}");
            }

        }

        // FIXED: New method to handle check action with passed data
        [NetworkCallable]
        public void AdvanceGameWithCheckData(string json)
        {

            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                // Deserialization succeeded! Let's figure out what we've got.
                if (result.TokenType == TokenType.DataDictionary)
                {
                    Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                    // Use the passed status instead of accessing local field
                    double statusDouble = result.DataDictionary["status"].Double;
                    double streetDouble = result.DataDictionary["street"].Double;


                    // Use the passed status instead of accessing local field
                    curStatus = (byte)statusDouble;
                    
                    Manager.SendToOwnerWithParam(nameof(GameManager.OnPlayerChoseOptionWithStatus), json);
                }
                else 
                {
                    Debug.LogError($"Unexpected result when deserializing json {json}");
                }
            } else {
                // Deserialization failed. Let's see what the error was.
                Debug.LogError($"Failed to Deserialize json {json} - {result.ToString()}");
            }
            
        }

        private void ClearDecision()
        {
            curStatus = GameManager.NoStatus;
            Manager.AddToConsole($"Changed curStatus to {curStatus} for player {PlayerNum}");
            Serialize();
        }

        private void ForceSmallBlind()
        {
            // You are forced to either go all in or to make the small blind bet
            int amount = Bankroll.GetChips() < GameManager.SmallBlind ? Bankroll.GetChips() : GameManager.SmallBlind;

            Bankroll.RemoveChips(amount);
            BetPile.AddChips(amount);

            bets[GameManager.PreflopStreet] = amount;

            Serialize();
        }

        private void ForceBigBlind()
        {
            // You are forced to either go all in or to make the big blind bet
            int amount = Bankroll.GetChips() < GameManager.BigBlind ? Bankroll.GetChips() : GameManager.BigBlind;

            Bankroll.RemoveChips(amount);
            BetPile.AddChips(amount);

            bets[GameManager.PreflopStreet] = amount;

            Serialize();
        }

        public void TakeRoundResults()
        {
            // Indicate that this pile of chips has moved to someone else (or self)
            BetPile.SetChips(0);

            // This player cannot participate in any pots if they folded
            if (Manager.PlayerFolded(PlayerNum))
                return;

            if (Manager.AllButOneFolded)
            {
                // We can safely take everyone else's money since we know for sure that everyone
                // but us has folded their cards

                Bankroll.AddChips(Manager.SumOfAllBets);
                winByDefault = true;
                WinnerFX.PlayForAll();
                Serialize();

                return;
            }

            mainPotWon = false;
            numPotsWon = 0;

            for (int curPot = 0; curPot < Manager.NumPots; ++curPot)
            {
                // When we reach one pot a player doesn't participate in, we know they don't participate in the rest
                if (!Manager.ParticipatesInPot(PlayerNum, curPot))
                    break;

                // REMARK: I don't care that the winners are being recalculated
                // because the calculations are local

                int[] winners = Manager.GetPotWinners(curPot);
                int potAmt = Manager.GetPot(curPot);

                if (potAmt <= 0)
                    continue;

                // If we didn't get a list or the number of winners is 0, something is VERY wrong
                if (winners == null || winners[0] == 0)
                {
                    Manager.AddToConsole($"THERE ARE NO WINNERS FOR POT {curPot}. SOMETHING HAS GONE HORRIBLY WRONG");
                    continue;
                }
                // If we are the only winner in the list, take the whole pot
                else if (winners[0] == 1 && winners[1] == PlayerNum)
                {
                    Bankroll.AddChips(potAmt);
                    
                    if (curPot == 0)
                        mainPotWon = true;
                    else
                        sidePotsWon[mainPotWon ? numPotsWon - 1 : numPotsWon] = curPot;
                    
                    numPotsWon++;
                }
                else
                {
                    // Make sure we are a winner before doing anything
                    bool isWinner = false;

                    // In case the split of the pot is odd and we need to give the odd chip(s) to someone
                    int closestWinner = winners[1];
                    int distToWinner = int.MaxValue;

                    for (int i = 0; i < winners[0]; ++i)
                    {
                        int curWinner = winners[i + 1];
                        int curDist = curWinner - Manager.CurDealerIndex;

                        if (curWinner == PlayerNum)
                            isWinner = true;

                        // If we found a player closer to the dealer (preferring those closest to the dealer
                        // in the direction of game flow), make them the closest player
                        if (curDist >= 0 && curDist < distToWinner)
                        {
                            closestWinner = curWinner;
                            distToWinner = curDist;
                        }
                    }

                    if (!isWinner)
                        continue;

                    // Split the pot
                    int partialAmt = potAmt / winners[0];
                    int remainderAmt = potAmt;

                    // REMARK: YOU'VE GOTTA BE [laxative]ING ME THEY DIDN'T EXPOSE THE MODULO OPERATOR???
                    // THAT'S A BASIC OPERATION YOU [friendly people]
                    for (int i = 0; i < winners[0]; ++i)
                        remainderAmt -= partialAmt;

                    // If we are the closest player, add the odd chip(s) to our partial amount
                    if (closestWinner == PlayerNum)
                        Bankroll.AddChips(partialAmt + remainderAmt);
                    // Otherwise only give the partial amount
                    else
                        Bankroll.AddChips(partialAmt);

                    if (curPot == 0)
                        mainPotWon = true;
                    else
                        sidePotsWon[numPotsWon - 1] = curPot;

                    numPotsWon++;
                }
            }

            if (numPotsWon > 0)
                WinnerFX.PlayForAll();

            Serialize();
        }

        public void JoinFullyFromLateness()
        {
            lateJoiner = false;
            Serialize();
        }

        private string BuildWinString()
        {
            if (winByDefault)
                return WinByDefaultStrings[LocalSettingsPanel.Settings.LanguageIndex];

            string result = "";

            for (int i = 0; i < numPotsWon; ++i)
            {
                if (mainPotWon)
                {
                    if (i == 0)
                    {
                        result += MainPotStrings[LocalSettingsPanel.Settings.LanguageIndex];

                        if (numPotsWon > 1)
                            result += SidePotWithMainStrings[LocalSettingsPanel.Settings.LanguageIndex];
                    }
                    else
                    {
                        if (i > 1)
                            result += ", ";
                        result += (LocalSettingsPanel.Settings.LanguageIndex == Translatable.LangEN || LocalSettingsPanel.Settings.LanguageIndex == Translatable.LangFI ? "#" : "") + sidePotsWon[i - 1].ToString();
                    }
                }
                else
                    result += (i == 0 ? SidePotNoMainStrings[LocalSettingsPanel.Settings.LanguageIndex] : ", ") + (LocalSettingsPanel.Settings.LanguageIndex == Translatable.LangEN || LocalSettingsPanel.Settings.LanguageIndex == Translatable.LangFI ? "#" : "") + sidePotsWon[i].ToString();
            }

            return result;
        }

        private string BuildShowdownStatus(bool revealing)
        {
            string sprite = revealing ? RevealedSprite : FoldedSprite;

            if (revealing && LocalSettingsPanel.Settings.LanguageIndex == Translatable.LangFI)
                sprite = $"<voffset=0.1em><size=60>{sprite}</size></voffset>";

            switch (LocalSettingsPanel.Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    return $"Votre main sera <b>{sprite} {(revealing ? "RÉVÉLÉE" : "COUCHÉE")}</b> dans :\n{ShowdownSubtitleSize}(Placez vos cartes dans la zone pour le faire vous-même.)";

                case Translatable.LangES:
                    return $"Su mano será <b>{sprite} {(revealing ? "PRESENTADA" : "ABANDONADA")}</b> en:\n{ShowdownSubtitleSize}(Coloque sus tarjetas en la zona para hacerlo usted mismo.)";

                case Translatable.LangDE:
                    return $"Ihre Hand wird <b>{sprite} {(revealing ? "PRÄSENTIERT" : "AUSSTEIGT")}</b> in:\n{ShowdownSubtitleSize}(Legen Sie Ihre Karten in den Bereich, um es selbst zu tun.)";

                case Translatable.LangJP:
                    return $"手札は             秒{(revealing ? $"後に<b>{sprite} 提示</b>" : $"で<b>{sprite} フォールド</b>")}される。\n                         {ShowdownSubtitleSize}(自分で行う場合はカードをエリアに置く。)";

                case Translatable.LangKR:
                    return $"<line-height=70%>핸드가 <b>{sprite} {(revealing ? "공개" : "폴드")}</b> 될때까지 남은 시간:\n{ShowdownSubtitleSize}(혼자 진행하기 위해 카드를 올려 놓으세요.)";

                case Translatable.LangFI:
                    return $"Kätesi <b>{sprite} {(revealing ? "NÄYTETÄÄN" : "KIPATAAN")}</b>:\n{ShowdownSubtitleSize}(Aseta kortit alueen sisään tehdäksesi sen itse.)";

                default:
                    return $"Your hand will be <b>{sprite} {(revealing ? "REVEALED" : "FOLDED")}</b> in:\n{ShowdownSubtitleSize}(Place your cards in the area to do it yourself.)";
            }
        }

        private string BuildLeaveGameCODA()
        {
            if (!Manager.GameStarted)
                return "";

            bool midgameJoinOn = Manager.MidgameJoining;

            switch (LocalSettingsPanel.Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    return $"Tout progrès sera perdu. Vous {(midgameJoinOn ? "pouvez" : "ne pourrez pas")} rejoindre en cours de partie.";

                case Translatable.LangES:
                    return $"Se perderán todos los progresos. {(midgameJoinOn ? "Puedes" : "No podrás")} reincorporarte a mitad de partida.";

                case Translatable.LangDE:
                    return $"Alle Fortschritte werden verloren gehen. Sie können{(midgameJoinOn ? "" : " nicht")} mitten im Spiel wieder einsteigen.";

                case Translatable.LangJP:
                    return $"すべての進歩が失われる。ゲーム{(midgameJoinOn ? "中盤で再合流できる" : "の途中で再参加することはできない")}。";
                
                case Translatable.LangKR:
                    return $"모든 진행 상황이 사라집니다. 게임중에 다시 참가 하실 수 {(midgameJoinOn ? "있" : "없")}습니다.";

                case Translatable.LangFI:
                    return $"Kaikki edistys menetetään. {(midgameJoinOn ? "Voit" : "Et voi")} liittyä takaisin kesken pelin.";

                default:
                    return $"All progress will be lost. {(midgameJoinOn ? "You can rejoin midgame." : "You will not be able to rejoin midgame.")}";
            }
        }
    }
}
