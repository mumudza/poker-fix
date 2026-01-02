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
    [RequireComponent(typeof(VRCPickup))]
    [RequireComponent(typeof(VRCObjectSync))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class FidgetBet : ChipContainer
    {
        [Header("Fidget Bet Parts (Must not be relatives)")]

        public Player ParentPlayer;
        public VRCPickup Pickup;
        public VRCObjectSync ObjectSync;
        public SFXPlayer PickupSFX, PutdownSFX;
        public Collider PickupCollider;

        public LayeredEffectPlayer[] PickupParticles;
        public LayeredEffectPlayer[] DropParticles;

        private readonly string[] FidgetStrings =
        {
              "Fidget with Chips"
            , "Jouer avec les jetons"
            , "Juguetear con fichas"
            , "Mit Jetons herumzappeln"
            , "チップスでそわそわ"
            , "칩을 갖고 놀기"
            , "Pelimarkoilla leikkimiseen"
        };

        public void Update()
        {
            if (Pickup.IsHeld)
            {
                if (DropParticles != null && DropParticles.Length > 0)
                {
                    foreach (var system in DropParticles)
                        system.gameObject.transform.position = gameObject.transform.position;
                }
                PutdownSFX.gameObject.transform.position = gameObject.transform.position;
            }
        }

        public override void OnPickup()
        {
            SetChips(1);
            PickupSFX.PlayForAll();

            foreach (var system in PickupParticles)
                system.PlayForAll();
        }

        public override void OnDrop()
        {
            foreach (var system in DropParticles)
                system.PlayForAll();

            SetChips(0);

            ObjectSync.FlagDiscontinuity();
            ObjectSync.Respawn();

            PutdownSFX.PlayForAll();
        }

        public void Respawn()
        {
            SetChips(0);

            ObjectSync.FlagDiscontinuity();
            ObjectSync.Respawn();
        }

        public override void Deserialize()
        {
            base.Deserialize();

            bool pickupable = OwnedByLocal && !ParentPlayer.Manager.Processing && (!ParentPlayer.LocalsTurn || ParentPlayer.Manager.CurStreet == GameManager.ShowdownStreet) && ParentPlayer.Bankroll.GetChips() > 0;

            if (!pickupable && Pickup.currentPlayer == LocalPlayer && Pickup.IsHeld)
                Pickup.Drop();

            Pickup.pickupable = pickupable;
            PickupCollider.enabled = pickupable;

            Pickup.InteractionText = FidgetStrings[ParentPlayer.LocalSettingsPanel.Settings.LanguageIndex];
        }
    }
}
