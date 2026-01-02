// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [RequireComponent(typeof(Animator))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedAnimator : Benscript
    {
        public const string NoAnimation = "";

        [UdonSynced]
        private string requestedAnim = NoAnimation;
        [UdonSynced]
        private uint requestedAnimLoops = 0;

        private string curAnim = NoAnimation;
        private uint curAnimLoops = 0;

        private Animator animator;

        public string CurrentAnimation => curAnim;

        public override void Start()
        {
            animator = GetComponent<Animator>();

            base.Start();
        }

        public void RequestAnimation(string anim)
        {
            if (!OwnedByLocal)
                return;

            if (anim == "Turn")
                Debug.Log($"Turning {gameObject.name}");

            if (anim == requestedAnim)
                ++requestedAnimLoops;
            else
            {
                requestedAnim = anim;
                requestedAnimLoops = 0;
            }

            Serialize();
        }

        public override void Deserialize()
        {
            base.Deserialize();

            if (requestedAnim != curAnim || requestedAnimLoops != curAnimLoops)
            {
                animator.ResetTrigger(curAnim);

                curAnim = requestedAnim;
                curAnimLoops = requestedAnimLoops;

                if (requestedAnim == NoAnimation)
                    return;

                animator.SetTrigger(requestedAnim);
            }
        }
    }
}
