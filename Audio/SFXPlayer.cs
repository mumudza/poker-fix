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
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SFXPlayer : UdonSharpBehaviour
    {
        public AudioClip[] Clips;

        [Range(-3f, 3f)]
        public float DefaultPitch = 1f;

        [Range(0, 3f)]
        public float PitchUpVariance = 0f;

        [Range(0, 3f)]
        public float PitchDownVariance = 0f;

        private AudioSource audioSource;
        private int[] shuffledIndices;
        private int curIndex = -1;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            shuffledIndices = new int[Clips.Length];

            for (int i = 0; i < Clips.Length; ++i)
                shuffledIndices[i] = i;

            Shuffle();
        }

        public void Play()
        {
            audioSource.pitch = DefaultPitch + Random.Range(-PitchDownVariance, PitchUpVariance);

            ++curIndex;

            if (curIndex >= Clips.Length)
            {
                curIndex -= Clips.Length;
                Shuffle();
            }

            audioSource.clip = Clips[curIndex];

            Debug.Log($"~~~~~~~~~~~~~ Playing {audioSource.clip.name} from {transform.parent.parent.gameObject.name} ~~~~~~~~~~~~~");
            audioSource.Stop();
            audioSource.Play();
        }

        public void PlayForAll() => SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Play));

        public void Stop() => audioSource.Stop();

        private void Shuffle() => Utilities.ShuffleArray(shuffledIndices);
    }
}
