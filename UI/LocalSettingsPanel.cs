// Author: ThisIsBennyK
// Copyright (c) 2024 ThisIsBennyK. All rights reserved.
// You may use the code as part of the VRChat Texas Hold'em game prefab in your world.
// You may modify the code at your own risk.
// You may not (re)distribute it.

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace ThisIsBennyK.TexasHoldEm
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalSettingsPanel : UdonSharpBehaviour
    {
        [Header("Local Settings Panel Parts")]

        public LocalSettings Settings;

        public Image CardPreview;
        public Dropdown CurrencyDropdown, LanguageDropdown;
        public Toggle ActionHintsToggle, HandNameToggle;
        public Button RespawnCardsButton;
        public Slider MusicVolumeSlider, SFXVolumeSlider;
        public SoundPool SFX;
        public TMPro.TextMeshProUGUI DesignCredits;
        public GameObject[] SettingsParts;
        public GameObject LegalCanvas, CreditsCanvas, ModerationCanvas;
        public Button ModerationButton;

        private bool EnoughPlayersForVoting => Settings.Manager.NumPlayers > GameManager.MinPlayersForVoting;

        public void _ChangeToNextCardDesign()
        {
            Settings.NextCardDesign();
            CardPreview.sprite = Settings.Manager.GameDeck.CardPreviewSprites[Settings.DesignIndex];
            SetDesignCredits();
        }

        public void _ChangeToPrevCardDesign()
        {
            Settings.PrevCardDesign();
            CardPreview.sprite = Settings.Manager.GameDeck.CardPreviewSprites[Settings.DesignIndex];
            SetDesignCredits();
        }

        public void _ChangeCurrencyCulture()
        {
            Settings.SetCurrencyCulture((uint)CurrencyDropdown.value);
        }

        public void _UseSlider() => Settings.SetInputMethod(LocalSettings.SliderInput);
        public void _UseNumpad() => Settings.SetInputMethod(LocalSettings.NumpadInput);

        public void _ChangeActionHintsEnabled() => Settings.SetActionHints(ActionHintsToggle.isOn);
        public void _ChangeHandNameEnabled() => Settings.SetHandName(HandNameToggle.isOn);

        public void MatchSettings()
        {
            CardPreview.sprite = Settings.Manager.GameDeck.CardPreviewSprites[Settings.DesignIndex];
            SetDesignCredits();

            CurrencyDropdown.SetValueWithoutNotify((int)Settings.CultureIndex);
            LanguageDropdown.SetValueWithoutNotify((int)Settings.LanguageIndex);
            ActionHintsToggle.SetIsOnWithoutNotify(Settings.ActionHintsEnabled);
            HandNameToggle.SetIsOnWithoutNotify(Settings.HandNameEnabled);

            MusicVolumeSlider.SetValueWithoutNotify(Settings.MusicVolume);
            SFXVolumeSlider.SetValueWithoutNotify(Settings.SFXVolume);

            Settings.Manager.Music.Volume = Settings.MusicVolume;
            SFX.Volume = Settings.SFXVolume;

            ModerationButton.interactable = EnoughPlayersForVoting;

            foreach (GameObject obj in SettingsParts)
                obj.SetActive(true);

            LegalCanvas.SetActive(false);
            CreditsCanvas.SetActive(false);
            ModerationCanvas.SetActive(false);
        }

        public void _ChangeMusicVolume()
        {
            Settings.MusicVolume = MusicVolumeSlider.value;
            Settings.Manager.Music.Volume = Settings.MusicVolume;
        }

        public void _ChangeSFXVolume()
        {
            Settings.SFXVolume = SFXVolumeSlider.value;
            SFX.Volume = Settings.SFXVolume;
            Settings.Manager._DeserializeLocally();
        }

        public void _ChangeLanguage()
        {
            Settings.SetLanguage((uint)LanguageDropdown.value);
            SetDesignCredits();
        }

        public void _OpenLegal()
        {
            foreach (GameObject obj in SettingsParts)
                obj.SetActive(false);
            LegalCanvas.SetActive(true);
        }

        public void _CloseLegal()
        {
            foreach (GameObject obj in SettingsParts)
                obj.SetActive(true);
            LegalCanvas.SetActive(false);
        }

        public void _OpenCredits()
        {
            foreach (GameObject obj in SettingsParts)
                obj.SetActive(false);
            CreditsCanvas.SetActive(true);
        }

        public void _CloseCredits()
        {
            foreach (GameObject obj in SettingsParts)
                obj.SetActive(true);
            CreditsCanvas.SetActive(false);
        }

        public void _OpenModeration()
        {
            if (!EnoughPlayersForVoting)
                return;

            foreach (GameObject obj in SettingsParts)
                obj.SetActive(false);
            ModerationCanvas.SetActive(true);
        }

        public void _CloseModeration()
        {
            foreach (GameObject obj in SettingsParts)
                obj.SetActive(true);
            ModerationCanvas.SetActive(false);
        }

        public void _UpdateModerationAvailability()
        {
            if (ModerationCanvas.activeSelf && !EnoughPlayersForVoting)
                MatchSettings();
            else if (!ModerationButton.interactable && EnoughPlayersForVoting)
                ModerationButton.interactable = true;
        }

        private void SetDesignCredits() => DesignCredits.text = Settings.Manager.GameDeck.CardDesignCredits[Settings.DesignIndex];
    }
}
