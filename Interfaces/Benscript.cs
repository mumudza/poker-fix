// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace ThisIsBennyK.TexasHoldEm
{
    public class Benscript : UdonSharpBehaviour
    {
        public const int InvalidPlayerID = -1;

        [Header("Benscript Properties")]
        public GameObject[] OwnershipRelatives;
        public bool ForceTransferOnDisconnect = false;

        [UdonSynced]
        private int currentOwnerID = InvalidPlayerID;

        private DataList postSerializationSignals = new DataList();

        public VRCPlayerApi LocalPlayer => Networking.LocalPlayer;
        public VRCPlayerApi Owner => Networking.GetOwner(gameObject);
        public bool OwnedByLocal => currentOwnerID == LocalID && Networking.IsOwner(gameObject);
        public int OwnerID => currentOwnerID;
        public int LocalID => Utilities.IsValid(LocalPlayer) ? LocalPlayer.playerId : InvalidPlayerID;
        public bool HasOwner => currentOwnerID != InvalidPlayerID && Utilities.IsValid(Owner) && currentOwnerID == Owner.playerId;

        public virtual void Start()
        {
            Deserialize();
        }

        public virtual void OwnByLocal()
        {
            Networking.SetOwner(LocalPlayer, gameObject);

            // TODO: Move the code below to OnOwnershipTransferred / new virtual function

            SetIDToLocal();
            Serialize();

            if (OwnershipRelatives == null || OwnershipRelatives.Length <= 0)
                return;

            foreach (GameObject obj in OwnershipRelatives)
            {
                UdonBehaviour udonBehaviour = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));

                if (udonBehaviour != null)
                    udonBehaviour.SendCustomEvent(nameof(OwnByLocal));
            }
        }
        public virtual void OwnByOther(VRCPlayerApi player)
        {
            Networking.SetOwner(player, gameObject);
            SendToOwner(nameof(OwnByLocal));
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (ForceTransferOnDisconnect && Networking.IsOwner(gameObject) && player == Owner && OwnerID != InvalidPlayerID && OwnerID != Owner.playerId)
            {
                Debug.Log($"{gameObject.name} force transferred");
                OwnByLocal();
                OnOwnershipForceTransferred();
            }
        }

        public virtual void OnOwnershipForceTransferred() { }

        public void SetIDToLocal() => currentOwnerID = LocalID;

        public virtual void Disown()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            if (OwnershipRelatives != null && OwnershipRelatives.Length > 0)
            {
                foreach (GameObject obj in OwnershipRelatives)
                {
                    UdonBehaviour udonBehaviour = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));

                    if (udonBehaviour != null)
                        udonBehaviour.SendCustomEvent(nameof(Disown));
                }
            }

            currentOwnerID = -1;
            Serialize();
        }

        public void SetRelativesActive(bool active)
        {
            if (OwnershipRelatives != null && OwnershipRelatives.Length > 0)
            {
                foreach (GameObject obj in OwnershipRelatives)
                    obj.SetActive(active);
            }
        }

        public void Serialize()
        {
            RequestSerialization();
            Deserialize();
        }

        public void AddPostSerialListener(string method) => postSerializationSignals.Add(method);

        public override void OnPostSerialization(SerializationResult result)
        {
            if (postSerializationSignals.Count > 0)
            {
                // Take all the signals currently in the list into a separate one for processing
                DataList signals = postSerializationSignals.DeepClone();

                // Clear the saved list for upcoming signals to be put in
                postSerializationSignals.Clear();

                foreach (DataToken token in signals.ToArray())
                    SendToOwner(token.String);
            }
        }

        // You cannot send OnDeserialization via SendCustomNetworkEvent
        public virtual void Deserialize()
        {
            if (OwnershipRelatives != null && OwnershipRelatives.Length > 0)
            {
                foreach (GameObject obj in OwnershipRelatives)
                {
                    UdonBehaviour udonBehaviour = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));

                    if (udonBehaviour != null)
                        udonBehaviour.SendCustomEvent(nameof(Deserialize));
                }
            }
        }

        public override void OnDeserialization() => Deserialize();

        public override void OnPlayerJoined(VRCPlayerApi player) => Deserialize();

        public void SendToOwner(string method)
        {
            if (OwnedByLocal)
                SendCustomEvent(method);
            else
                SendCustomNetworkEvent(NetworkEventTarget.Owner, method);
        }
        public void SendToAll(string method) => SendCustomNetworkEvent(NetworkEventTarget.All, method);
    }
}
