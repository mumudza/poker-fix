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
        private DataList postSerializationSignalsWithParams = new DataList();
        
        // Single serialized string parameter for network events [UdonSynced]
        private string[] serializedParams = new string[MAX_QUEUED_PARAM_EVENTS];
        private int serializedParamsIdx = 0;

        public VRCPlayerApi LocalPlayer => Networking.LocalPlayer;
        public VRCPlayerApi Owner => Networking.GetOwner(gameObject);
        public bool OwnedByLocal => currentOwnerID == LocalID && Networking.IsOwner(gameObject);
        public int OwnerID => currentOwnerID;
        public int LocalID => Utilities.IsValid(LocalPlayer) ? LocalPlayer.playerId : InvalidPlayerID;
        public bool HasOwner => currentOwnerID != InvalidPlayerID && Utilities.IsValid(Owner) && currentOwnerID == Owner.playerId;

        public virtual void Start()
        {
            serializedParams = new string[MAX_QUEUED_PARAM_EVENTS];
            for (int i = 0; i < MAX_QUEUED_PARAM_EVENTS; i++)
                serializedParams[i] = "";
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

        public void AddPostSerialListenerWithParam(string method, DataToken param)
        {
            string jsonParam = SerializeParameterToString(param);

            if (serializedParamsIdx >= MAX_QUEUED_PARAM_EVENTS) // overflow
            {
                Debug.LogError($"Sent out way too many parameterized events (>{MAX_QUEUED_PARAM_EVENTS})!!!");
                return;
            }

            postSerializationSignalsWithParams.Add(method);
            serializedParams[serializedParamsIdx] = jsonParam;
            serializedParamsIdx++;
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
            
            if (postSerializationSignalsWithParams.Count > 0)
            {
                // Take all the parameterized signals into a separate list for processing
                DataList paramSignals = postSerializationSignalsWithParams.DeepClone();
                postSerializationSignalsWithParams.Clear();

                int i = 0;
                foreach (DataToken token in paramSignals.ToArray())
                {   
                    SendToOwnerWithParam(token.String, serializedParams[i]);
                    serializedParams[i] = "";
                    i++;
                }
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

        private int GetStringSize(string str)
        {
            return (str.Length + 1) * 2;
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
    }
}
