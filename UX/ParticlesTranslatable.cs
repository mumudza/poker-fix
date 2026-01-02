// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UnityEngine;

namespace ThisIsBennyK.TexasHoldEm
{
    public class ParticlesTranslatable : Translatable
    {
        public ParticleSystem[] systems;

        public void Play() => systems[Settings.LanguageIndex].Play();
    }
}
