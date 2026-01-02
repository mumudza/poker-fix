// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SoundPool : UdonSharpBehaviour
    {
        public AudioSource[] Sources;

        public float Volume
        {
            get => volume;
            set
            {
                volume = value;

                foreach (AudioSource source in Sources)
                    source.volume = volume;
            }
        }

        private float volume = 1f;
    }
}
