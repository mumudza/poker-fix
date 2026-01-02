// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FollowerByRotation : UdonSharpBehaviour
    {
        [Range(1f, 25f)]
        public float RotationDecay = 16f;

        void Update()
        {
            if (!(gameObject.activeInHierarchy && Utilities.IsValid(Networking.LocalPlayer)))
                return;

            transform.GetPositionAndRotation(out Vector3 prevPos, out Quaternion prevRot);
            Transform playerXform = transform;

            playerXform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
            transform.LookAt(playerXform);

            Quaternion targetRot = transform.rotation;

            transform.SetPositionAndRotation(prevPos, DecayRot(prevRot, targetRot));
        }

        public Quaternion DecayRot(Quaternion cur, Quaternion target) => Quaternion.Lerp(target, cur, Mathf.Exp(-RotationDecay * Time.deltaTime));
    }
}
