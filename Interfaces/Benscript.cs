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
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDK3.UdonNetworkCalling;  

namespace ThisIsBennyK.TexasHoldEm
{
    public class Benscript : UdonSharpBehaviour
    {
        public const int InvalidPlayerID = -1;
        private const int MAX_QUEUED_PARAM_EVENTS = 50;

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

        // Acknowledgement System Fields
        private int waitingForAckCount = 0;
        public bool waitingForAck = false;
        private float ackTimeoutTimer = 0f;
        private const float ackTimeoutSeconds = 1.0f;

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

        protected string SerializeParameterToString(DataToken param)
        {
            if (VRCJson.TrySerializeToJson(param, JsonExportType.Beautify, out DataToken json))
            {
                // Successfully serialized! We can immediately get the string out of the token and do something with it.
                Debug.Log($"Successfully serialized to json: {json.String}");
                return json.String;
            } 
            else 
            {
                // Failed to serialize for some reason, running ToString on the result should tell us why.
                Debug.LogError(json.ToString());
                return "";
            }
        }

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

        public void SendToOwnerWithParam(string method, string param)
        {
            // VRC devs being stupid
            // SendCustomEvent doesnt exist (?)
            // w/e
            //if (OwnedByLocal)
            //    SendCustomEvent(method, param);
            //else
            SendCustomNetworkEvent(NetworkEventTarget.Owner, method, param);
        }

        public void SendToOwnerWithParam(string method, DataToken param)
        {
            string value = SerializeParameterToString(param);
            SendToOwnerWithParam(method, value);
        }
        
        public void SendToAll(string method) => SendCustomNetworkEvent(NetworkEventTarget.All, method);


        public void SendToAllWithParam(string method, string param)
        {
            // VRC devs being stupid
            // SendCustomEvent doesnt exist (?)
            // w/e
            //if (OwnedByLocal)
            //    SendCustomEvent(method, param);
            //else
            SendCustomNetworkEvent(NetworkEventTarget.All, method, param);
        }
        public void SendToAllWithParam(string method, DataToken param)
        {
            string value = SerializeParameterToString(param);
            SendToOwnerWithParam(method, value);
        }

        // ============================================
        // Acknowledgement System
        // ============================================

        /// <summary>
        /// Serializes data and waits for acknowledgements from players.
        /// Call this when you need to ensure players have received the serialized data.
        /// </summary>
        /// <param name="expectedAcks">Number of acknowledgements expected from players who own seats/objects</param>
        /// <returns>True if waiting for acknowledgements, false otherwise</returns>
        public bool SerializeOwnerSync(int expectedAcks)
        {
            waitingForAck = true;
            waitingForAckCount = expectedAcks;
            ackTimeoutTimer = ackTimeoutSeconds;
            
            Serialize();
            
            return waitingForAck;
        }

        /// <summary>
        /// Updates the acknowledgement flag and handles timeout.
        /// Call this from your derived class's Update() method.
        /// </summary>
        public void UpdateAckFlag()
        {
            if (!waitingForAck)
                return;

            ackTimeoutTimer -= Time.deltaTime;

            if (ackTimeoutTimer <= 0f)
            {
                Debug.LogWarning($"{gameObject.name}: Acknowledgement timeout after {ackTimeoutSeconds} seconds");
                waitingForAck = false;
                waitingForAckCount = 0;
            }
        }

        /// <summary>
        /// Network event sent to specific players requesting acknowledgement.
        /// Players should only acknowledge if they own a seat or player object.
        /// </summary>
        [NetworkCallable]
        public void RequestAckForOwnerSync(string ackFunction)
        {
            if (HasOwner)
            {
                SendCustomNetworkEvent(NetworkEventTarget.Owner, ackFunction);
            }
        }

        /// <summary>
        /// Network event sent back to owner when a player acknowledges.
        /// </summary>
        [NetworkCallable]
        public void AcknowledgeOwnerSync()
        {
            if (!waitingForAck)
                return;

            waitingForAckCount--;

            if (waitingForAckCount <= 0)
            {
                waitingForAck = false;
                waitingForAckCount = 0;
                Debug.Log($"{gameObject.name}: All acknowledgements received");
            }
        }
    }
}
