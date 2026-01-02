// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SyncedObjectPool : Benscript
    {
        public GameObject[] Objects;
        public bool DeactivateWithoutOwnership = true;

        [UdonSynced]
        private int[] spawnOrder;

        [UdonSynced]
        private int curSpawned = -1;

        [UdonSynced]
        private int[] owners;

        [UdonSynced]
        private bool initialized = false;

        public bool AnySpawned
        {
            get
            {
                if (curSpawned != -1)
                    return true;

                foreach (int owner in owners)
                {
                    if (owner != InvalidPlayerID)
                        return true;
                }

                return false;
            }
        }

        public override void Start()
        {
            if (initialized)
            {
                base.Start();
                return;
            }

            spawnOrder = new int[Objects.Length];
            owners = new int[Objects.Length];

            for (int i = 0; i < Objects.Length; ++i)
            {
                spawnOrder[i] = i;
                owners[i] = InvalidPlayerID;
            }

            initialized = true;

            Serialize();
        }

        public override void Deserialize()
        {
            base.Deserialize();

            if (DeactivateWithoutOwnership)
            {
                for (int i = 0; i < Objects.Length; ++i)
                    Objects[i].SetActive(owners[i] != InvalidPlayerID);
            }
        }

        public void Shuffle()
        {
            Utilities.ShuffleArray(spawnOrder);
            Serialize();
        }

        public void RequestObjectsFor(VRCPlayerApi player, int numObjects)
        {
            // If we've already spawned all the objects, don't do anything
            if (curSpawned == Objects.Length - 1)
                return;

            for (int i = 0; i < numObjects; ++i)
            {
                // Check again for if we've spawned all the obejcts since that could change in the loop
                if (curSpawned == Objects.Length - 1)
                    break;

                // Move to the next object index in the list...
                ++curSpawned;

                // ...and spawn and give the object to the other player

                int objIdx = spawnOrder[curSpawned];

                TransferOwnershipTo(player, objIdx);
            }

            Serialize();
        }

        public int[] GetOwnedObjectIndicesFor(int playerID)
        {
            // If we haven't spawned any objects, don't do anything
            if (curSpawned == -1)
                return null;

            // Create enough space to accumulate all of the player's objects
            // The maximum set of objects a player could have is all of them
            int[] accumulator = new int[Objects.Length];
            int numAccumulated = -1;

            for (int i = 0; i < Objects.Length; ++i)
            {
                // If this object is owned by this player...
                if (owners[i] == playerID)
                {
                    // Add its to the accumulated list of indices
                    ++numAccumulated;
                    accumulator[numAccumulated] = i;
                }
            }

            // If we didn't find any objects, don't continue
            if (numAccumulated == -1)
                return null;

            // Reduce the size of the returned array by creating a new array that fits exactly
            // the number of objects the player has and copying the indices from the accumulator into it

            int[] finalIdxArray = new int[numAccumulated + 1];

            for (int i = 0; i < numAccumulated + 1; ++i)
                finalIdxArray[i] = accumulator[i];

            return finalIdxArray;
        }

        public GameObject TryToGetObjectFor(int playerID, int idx)
        {
            if (idx < 0 || idx >= owners.Length || owners[idx] != playerID)
                return null;
            return Objects[idx];
        }

        public void TransferOwnershipTo(VRCPlayerApi player, params int[] indices)
        {
            foreach (int index in indices)
            {
                GameObject obj = Objects[index];
                UdonBehaviour udonBehaviour = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));

                Networking.SetOwner(player, obj);

                if (udonBehaviour != null)
                    udonBehaviour.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OwnByLocal));

                owners[index] = player.playerId;

                Debug.Log($"!!! Pool {gameObject.name}: Transferred {obj.name} to {player.displayName} !!!");
            }
            Serialize();
        }

        public void ReturnAll()
        {
            curSpawned = -1;

            for (int i = 0; i < Objects.Length; ++i)
            {
                owners[i] = InvalidPlayerID;

                GameObject obj = Objects[i];
                UdonBehaviour udonBehaviour = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
                VRCObjectSync objectSync = (VRCObjectSync)obj.GetComponent(typeof(VRCObjectSync));

                if (objectSync != null)
                {
                    objectSync.FlagDiscontinuity();
                    objectSync.Respawn();
                }

                if (udonBehaviour != null)
                    udonBehaviour.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(Disown));
            }

            Serialize();
        }

        public bool IsOwned(int index) => owners[index] != InvalidPlayerID;
    }
}
