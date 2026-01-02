// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SyncedEventTimer : Benscript
    {
        public Benscript BSBehaviour;
        public string Method;

        [UdonSynced]
        public float TimeInSeconds;
        
        public bool ToOwner = true;

        [UdonSynced]
        private float timer = 0f;

        public float TimeRemaining => timer;
        public bool TimerActive => timer > 0f;

        public void FixedUpdate()
        {
            if (!(OwnedByLocal && TimerActive))
                return;

            timer -= Time.fixedDeltaTime;

            if (!TimerActive)
            {
                if (ToOwner)
                    BSBehaviour.SendToOwner(Method);
                else
                    BSBehaviour.SendToAll(Method);
            }
        }

        public void StartTimer() => timer = TimeInSeconds;
        public void CancelTimer() => timer = 0f;
    }
}
