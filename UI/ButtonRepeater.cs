// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(EventTrigger))]
    public class ButtonRepeater : UdonSharpBehaviour
    {
        public float Delay = 0.0f;
        public float Repeat = 1.0f;

        public UdonSharpBehaviour[] behaviours;
        public string[] events;

        private bool buttonPressed;

        private float timer = 0.0f;

        private void FixedUpdate()
        {
            if (!buttonPressed || !(behaviours != null && behaviours.Length > 0 && events != null && events.Length > 0))
                return;

            if (timer > 0.0f)
                timer -= Time.fixedDeltaTime;

            if (timer <= 0.0f)
            {
                for (int i = 0; i < behaviours.Length; i++)
                    behaviours[i].SendCustomEvent(events[i]);

                timer = Repeat;
            }
        }

        public void OnButtonPressed()
        {
            buttonPressed = true;

            if (behaviours != null && behaviours.Length > 0 && events != null && events.Length > 0)
                timer = Delay;
        }
        public void OnButtonReleased() => buttonPressed = false;
    }
}
