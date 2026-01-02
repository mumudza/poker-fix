// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ActionArea : Benscript
    {
        [Header("Action Area Parts (Must not be relatives)")]
        public GameManager Manager;
        public Collider Area;
        public GameObject Perimeter;
        public MeshRenderer Glow;

        [Header("Action Area Data")]
        [Range(1f, 25f)]
        public float FadeDecay = 16f;

        private bool ActionsAllowed =>
            Manager.GameStarted
            && !Manager.Processing
            && Manager.CurPlayer.OwnedByLocal
            && Manager.CurPlayer.LocalsTurn
            && !Manager.CurPlayer.DecisionMade;

        public override void Start()
        {
            base.Start();
            Glow.material.color = new Color(1f, 1f, 1f, 0f);
        }

        public void Update()
        {
            Glow.material.color = new Color(1f, 1f, 1f, DecayOpacity(Glow.material.color.a));
        }

        public override void Deserialize()
        {
            base.Deserialize();
            Area.enabled = ActionsAllowed;
            Perimeter.SetActive(ActionsAllowed);
            Glow.enabled = Manager.GameStarted;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (IsHand(other))
            {
                Manager.AddToConsole("Hand in area!");
                other.gameObject.GetComponent<Hand>().InArea = true;
            }
            else if (IsAddtBet(other))
            {
                Manager.AddToConsole("Bet in area!");
                other.gameObject.GetComponent<AdditionalBet>().InArea = true;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (IsHand(other))
            {
                Manager.AddToConsole("Hand NOT in area!");
                other.gameObject.GetComponent<Hand>().InArea = false;
            }
            else if (IsAddtBet(other))
            {
                Manager.AddToConsole("Bet NOT in area!");
                other.gameObject.GetComponent<AdditionalBet>().InArea = false;
            }
        }

        public bool IsHand(Collider other)
        {
            Hand hand = other.gameObject.GetComponent<Hand>();

            if (hand != null)
                return hand.OwnedByLocal;
            return false;
        }

        public bool IsAddtBet(Collider other)
        {
            AdditionalBet additionalBet = other.gameObject.GetComponent<AdditionalBet>();

            if (additionalBet != null)
                return additionalBet.OwnedByLocal;
            return false;
        }

        public float DecayOpacity(float val)
        {
            float target = ActionsAllowed && ((Manager.CurPlayer.Hand.InArea && Manager.CurPlayer.Hand.Pickup.IsHeld) || Manager.CurPlayer.AddtBet.InArea) ? 1f : 0f;
            return Mathf.Lerp(target, val, Mathf.Exp(-FadeDecay * Time.deltaTime));
        }
    }
}
