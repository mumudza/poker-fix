// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UnstartedInfoDisplay : UdonSharpBehaviour
    {
        public const string WarningSign = "     ⚠", ReadySign = "☑";
        public const string NoWinners = "---";

        private readonly string[] MorePlayersMessages =
        {
              "At least 2 players are required."
            , "Au moins 2 joueurs sont nécessaires."
            , "Se necesitan al menos 2 jugadores."
            , "Es werden mindestens 2 Spieler benötigt."
            , "最低2人の選手が必要。"
            , "최소 플레이어 2명이 필요합니다."
            , "Vähintään 2 pelaajaa vaaditaan."
        };

        private readonly string[] ReadyMessages =
        {
              "Start when ready, 👑 game master!"
            , "Commencez quand vous êtes prêt, 👑 meneur·se de jeu !"
            , "¡Comienza cuando estés listo, 👑 maestro del juego!"
            , "Starte, wenn Sie bereit ist, 👑 Spielleiter!"
            , "👑ゲームマスター、準備ができたら始めよう！"
            , "준비 되셨으면 시작하세요, 👑 게임 마스터!"
            , "Aloita kun valmis, 👑 pelimestari!"
        };

        [Header("Unstarted Info Display Parts")]

        public GameManager Manager;

        public TMPro.TextMeshProUGUI NumPlayers, MessageSign, Message, PrevWinners;

        public void SetInfo()
        {
            NumPlayers.text = "" + Manager.NumPlayers + " / " + Manager.Players.Length;

            if (Manager.HasEnoughPlayers)
            {
                MessageSign.text = ReadySign;
                Message.text = ReadyMessages[Manager.LocalSettings.LanguageIndex];
            }
            else
            {
                MessageSign.text = WarningSign;
                Message.text = MorePlayersMessages[Manager.LocalSettings.LanguageIndex];
            }

            if (Manager.PrevWinners != "")
                PrevWinners.text = Manager.PrevWinners;
            else
                PrevWinners.text = NoWinners;
        }
    }
}
