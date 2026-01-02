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
    public class AdditionalBet : ChipContainer
    {
        [Header("Additional Bet Parts (Must not be relatives)")]

        public Player ParentPlayer;
        public VRCPickup Pickup;
        public VRCObjectSync ObjectSync;
        public SFXPlayer PickupSFX, PutdownSFX;
        public Collider PickupCollider;

        public LayeredEffectPlayer[] PickupParticles;
        public LayeredEffectPlayer[] DropParticles;
        public LayeredEffectPlayer[] InAreaParticles;
        public LayeredEffectPlayer[] OutsideAreaParticles;

        [HideInInspector]
        public bool InArea = false;

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
            SetChips(ParentPlayer.Bankroll.BetAmount);
            ParentPlayer.Bankroll.RemoveChips(GetChips());
            PickupSFX.PlayForAll();

            foreach (var system in PickupParticles)
                system.PlayForAll();
        }

        public override void OnDrop()
        {
            if (ParentPlayer.ActionsAllowed && InArea)
            {
                ParentPlayer.Manager.AddToConsole("Bet choice confirmed");
                ParentPlayer.OnBetActionChosen();
                DisplayParticles(InAreaParticles);
            }
            else
            {
                ParentPlayer.Bankroll.AddChips(GetChips());
                ParentPlayer.Bankroll.SetBetAmount(GetChips());

                foreach (var system in OutsideAreaParticles)
                    system.PlayForAll();
            }

            InArea = false;

            DisplayParticles(DropParticles);
            SetChips(0);

            ObjectSync.FlagDiscontinuity();
            ObjectSync.Respawn();

            PutdownSFX.PlayForAll();
        }

        public void Respawn()
        {
            ParentPlayer.Bankroll.AddChips(GetChips());

            InArea = false;
            SetChips(0);

            ObjectSync.FlagDiscontinuity();
            ObjectSync.Respawn();
        }

        public override void Deserialize()
        {
            base.Deserialize();

            bool pickupable = OwnedByLocal && !ParentPlayer.Manager.Processing && ParentPlayer.LocalsTurn && !ParentPlayer.DecisionMade && ParentPlayer.Manager.CurStreet != GameManager.ShowdownStreet;

            Pickup.pickupable = pickupable;
            PickupCollider.enabled = pickupable;
        }

        private void DisplayParticles(LayeredEffectPlayer[] particles)
        {
            int bet = GetChips();
            int bankroll = ParentPlayer.Bankroll.GetChips() + bet;
            int maxParticleIdx = Mathf.CeilToInt(bet / (float)bankroll * particles.Length);

            for (int i = 0; i < maxParticleIdx; ++i)
                particles[i].PlayForAll();
        }
    }
}
