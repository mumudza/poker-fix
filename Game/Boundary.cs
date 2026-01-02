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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Boundary : UdonSharpBehaviour
    {
        public VRCObjectSync[] ConstrainedObjects;

        [UdonSynced]
        private bool[] withinSelf;

        public void Start()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            withinSelf = new bool[ConstrainedObjects.Length];

            for (int i = 0; i < withinSelf.Length; ++i)
                withinSelf[i] = true;

            RequestSerialization();
        }

        public void Update()
        {
            UpdateObjects();
        }

        public void OnTriggerEnter(Collider other)
        {
            UpdateFlags(other.gameObject, true);
        }

        public void OnTriggerExit(Collider other)
        {
            UpdateFlags(other.gameObject, false);
        }

        public void UpdateFlags(GameObject other, bool flag)
        {
            if (ConstrainedObjects == null || ConstrainedObjects.Length == 0)
                return;

            for (int i = 0; i < ConstrainedObjects.Length; ++i)
            {
                if (ConstrainedObjects[i].gameObject == other)
                {
                    withinSelf[i] = flag;
                    return;
                }
            }

            RequestSerialization();
        }

        public void UpdateObjects()
        {
            if (ConstrainedObjects == null || ConstrainedObjects.Length == 0)
                return;

            for (int i = 0; i < ConstrainedObjects.Length; ++i)
            {
                VRCObjectSync objectSync = ConstrainedObjects[i];

                VRCPickup pickup = (VRCPickup)objectSync.gameObject.GetComponent(typeof(VRCPickup));

                if (pickup != null && pickup.IsHeld)
                    return;

                if (!withinSelf[i])
                {
                    objectSync.FlagDiscontinuity();
                    objectSync.Respawn();
                }
            }
        }
    }
}
