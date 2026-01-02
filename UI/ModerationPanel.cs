// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ModerationPanel : Benscript
    {
        public const string NoPlayer = "---";
        public const string NoVotesNeeded = "-";

        private readonly string[] VotesNeededStrings =
        {
              "Votes needed to kick: "
            , "Votes nécessaires pour l’expulsion : "
            , "Votos necesarios para expulsar: "
            , "Für den Rauswurf werden Stimmen benötigt: "
            , "退場に必要な投票数："
            , "플레이어 방출에 필요한 투표수: "
            , "Ääniä pelaajan poistamiseen tarvitaan: "
        };

        [Header("Moderation Panel Parts (must not be relatives)")]

        public Player ParentPlayer;
        public Toggle[] VoteToggles;
        public TMPro.TextMeshProUGUI[] InfoTexts;
        public TMPro.TextMeshProUGUI VotesNeededText;

        [UdonSynced]
        private bool[] votes;

        public override void Start()
        {
            votes = new bool[ParentPlayer.Manager.Players.Length];

            base.Start();
        }

        public override void Deserialize()
        {
            base.Deserialize();

            VotesNeededText.text = VotesNeededStrings[ParentPlayer.LocalSettingsPanel.Settings.LanguageIndex] + (ParentPlayer.Manager.NumPlayers > 1 ? "" + ParentPlayer.Manager.VoteMajority : NoVotesNeeded);

            if (votes == null || votes.Length == 0)
            {
                foreach (Toggle toggle in VoteToggles)
                {
                    toggle.interactable = false;
                    toggle.SetIsOnWithoutNotify(false);
                }

                foreach (var textbox in InfoTexts)
                    textbox.text = NoPlayer;

                return;
            }

            int curVoteMajority = ParentPlayer.Manager.VoteMajority;

            for (int i = 0; i < votes.Length; ++i)
            {
                Player player = ParentPlayer.Manager.Players[i];
                int numVotes = ParentPlayer.Manager.GetVotes(i);
                bool atTable = player.HasOwner;

                // REMARK: Why I am writing data in what is expected to be a read-only function?
                // The problem is that we want to be positive that the votes are cleared for somebody who is just joining the table,
                // but if we do it as we do in other functions, the data can't be synced fast enough to guarantee this.
                // So, when a player leaves, Deserialize() is called. The votes are modified such that if a player left,
                // their votes are cleared for everybody, but not serialized until [...]
                votes[i] = atTable && votes[i];

                VoteToggles[i].interactable = atTable && !player.OwnedByLocal && !(ParentPlayer.Manager.GameStarted && ParentPlayer.Manager.Processing) && numVotes < curVoteMajority;
                VoteToggles[i].SetIsOnWithoutNotify(votes[i]);
                InfoTexts[i].text = atTable ? (numVotes > 0 ? $"({numVotes}/{curVoteMajority}) " : string.Empty) + player.Owner.displayName : NoPlayer;
            }
        }

        public void _UpdateVotes()
        {
            for (int i = 0; i < votes.Length; ++i)
                votes[i] = VoteToggles[i].isOn;

            AddPostSerialListener(nameof(OnVotesUpdated));
            Serialize();
        }

        public void OnVotesUpdated() => ParentPlayer.Manager.SendToOwner(nameof(GameManager.UpdateModeration));

        public bool GetVote(int index) => votes != null && votes[index];

        public void ClearVotesForNewJoiners()
        {
            // [...] the moment we need them to be, which is now, when a new player joins and their vote needs to be cleared.
            // I call this "the chain reaction." This contrived bullshit is due to the fact that
            // networked synchronization of data is inherently slow, and if we saved this data in other ways,
            // the moderation panel would not update precisely when a player joins.
            // Is this good code? Hell no, absolutely not. But it works, and that's what matters.

            Serialize();
        }
    }
}
