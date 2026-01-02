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
    public class LayeredEffectPlayer : UdonSharpBehaviour
    {
        public ParticleSystem[] Particles;
        public ParticlesTranslatable[] Translatables;
        public SFXPlayer[] SoundEffects;

        private bool dontPlayForCaller = false;

        public void Play()
        {
            if (dontPlayForCaller)
            {
                dontPlayForCaller = false;
                return;
            }

            foreach (var system in Particles)
                system.Play();

            if (Translatables != null && Translatables.Length > 0)
            {
                foreach (var system in Translatables)
                    system.Play();
            }

            foreach (var effect in SoundEffects)
                effect.Play();
        }

        public void PlayForAll() => SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Play));
        public void PlayForAllExceptMe()
        {
            dontPlayForCaller = true;
            PlayForAll();
        }

        public void Stop()
        {
            foreach (var system in Particles)
                system.Stop();
        }
    }
}
