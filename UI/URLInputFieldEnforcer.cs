// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class URLInputFieldEnforcer : UdonSharpBehaviour
    {
        public VRCUrlInputField Input;

        [SerializeField]
        private VRCUrl Link;

        void Start()
        {
            Input.SetUrl(Link);
        }
    }
}
