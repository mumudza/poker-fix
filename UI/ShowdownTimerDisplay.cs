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
    [RequireComponent(typeof(Slider))]
    public class ShowdownTimerDisplay : UdonSharpBehaviour
    {
        public SyncedEventTimer ShowdownTimer;
        public TMPro.TextMeshProUGUI TimeText;
        public Vector3 SliderInitialScale, TextInitialScale;
        public float ScaleAmount = 1f;

        private Slider slider;

        public void Start()
        {
            slider = GetComponent<Slider>();
        }

        public void FixedUpdate()
        {
            if (!(ShowdownTimer.OwnedByLocal && ShowdownTimer.TimerActive))
                return;

            float percentageAdjustedTime = (ShowdownTimer.TimeRemaining - Player.AutoAdvanceCoyoteTime) / (ShowdownTimer.TimeInSeconds - Player.AutoAdvanceCoyoteTime);

            slider.SetValueWithoutNotify(Mathf.Clamp01(percentageAdjustedTime));
            TimeText.text = $"{Mathf.CeilToInt(Mathf.Clamp(ShowdownTimer.TimeRemaining - Player.AutoAdvanceCoyoteTime, 0f, ShowdownTimer.TimeInSeconds - Player.AutoAdvanceCoyoteTime))}";

            float scale = Mathf.Cos(Mathf.PI * 2 * ShowdownTimer.TimeRemaining);

            slider.transform.localScale = SliderInitialScale + scale * ScaleAmount * SliderInitialScale;
            TimeText.transform.localScale = TextInitialScale + scale * ScaleAmount * TextInitialScale;
        }
    }
}
