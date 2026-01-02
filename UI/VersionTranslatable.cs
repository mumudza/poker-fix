// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace ThisIsBennyK.TexasHoldEm
{
    public class VersionTranslatable : Translatable
    {
        public const int VersionMajor = 1, VersionMinor = 2, VersionPatch = 0;

        private readonly string[] LatestVersionStrings =
        {
              "Latest Version"
            , "Dernière Version"
            , "Última versión"
            , "Neueste Version"
            , "最新バージョン"
            , "최신 버전"
            , "Viimeisin Versio"
        };
        private readonly string[] NewUpdateStrings =
        {
              "New Update Available!"
            , "Nouvelle mise à jour disponible !"
            , "¡Nueva actualización disponible!"
            , "Neue Aktualisierung verfügbar!"
            , "新しいアップデート！"
            , "업데이트 가능!"
            , "Uusi Päivitys Saatavilla!"
        };

        public TMPro.TextMeshProUGUI StatusText;
        public Color LatestVersionColor, NewUpdateColor, ErrorColor;

        [SerializeField]
        private VRCUrl VersionInfo;

        private string VersionString => $"{VersionMajor}.{VersionMinor}.{VersionPatch}";
        private string LabeledVersionString => $"v{VersionMajor}.{VersionMinor}.{VersionPatch}";

        private bool downloading = true, failed = false, newVersion = false;
        private string downloadResult = "";

        public override void Start()
        {
            base.Start();
            VRCStringDownloader.LoadUrl(VersionInfo, (IUdonEventReceiver)this);
        }

        public override void Translate()
        {
            Color textColor;

            if (failed)
                textColor = ErrorColor;
            else if (newVersion)
                textColor = NewUpdateColor;
            else
                textColor = LatestVersionColor;

            string colorString = $"<color=#{(uint)(textColor.r * 255f):X}{(uint)(textColor.g * 255f):X}{(uint)(textColor.b * 255f):X}>";

            if (downloading)
                StatusText.text = LabeledVersionString + "\n...";
            else if (failed)
                StatusText.text = LabeledVersionString + $"\n{colorString}Version check failed: " + downloadResult;
            else if (newVersion)
                StatusText.text = LabeledVersionString + "\n" + colorString + NewUpdateStrings[Settings.LanguageIndex] + " v" + downloadResult;
            else
                StatusText.text = LabeledVersionString + "\n" + colorString + LatestVersionStrings[Settings.LanguageIndex];
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            string data = result.Result;

            downloading = false;

            if (VRCJson.TryDeserializeFromJson(data, out DataToken json))
            {
                if (json.TokenType == TokenType.DataDictionary)
                {
                    if (json.DataDictionary.TryGetValue("version", TokenType.String, out DataToken value))
                    {
                        newVersion = value != VersionString;
                        downloadResult = value.String;
                    }
                    else
                    {
                        failed = true;
                        downloadResult = $"(4) {value.TokenType}{(value.TokenType == TokenType.Error ? $" - {value.Error}" : "")}";
                    }
                }
                else
                {
                    failed = true;
                    downloadResult = $"(3) {json.TokenType}{(json.TokenType == TokenType.Error ? $" - {json.Error}" : "")}";
                }
            }
            else
            {
                failed = true;
                downloadResult = $"(2) {json.Error}";
            }

            Translate();
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            downloading = false;
            failed = true;
            downloadResult = $"(1) {result.ErrorCode} ({result.Error})";

            Translate();
        }
    }
}
