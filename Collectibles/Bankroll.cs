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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Bankroll : ChipContainer
    {
        public const string BetEmoji = "<sprite=1>";
        public const string BankEmoji = "<sprite=2>";
        public const string ChipLeaderEmoji = "<sprite=9>";
        public const string NumpadValidText = "✔️", NumpadInvalidText = "❌";
        public const int Minimum = 5000, Maximum = 20000;

        private readonly string[] BetVerbs =
        {
              "Bet "
            , "Parier "
            , "Apostar "
            , "Wetten "
            , "ベット"
            , "베트하기 "
            , "Panos "
        };

        [Header("Bankroll Parts (Must not be relatives)")]

        public Player ParentPlayer;
        public GameObject BetCanvas, LimitedActionCanvas;
        public Slider BetSlider;
        public TMPro.TextMeshProUGUI BetText, CallAndAllInOnly, CallOnly, AllInOnly;

        public GameObject NumpadCanvas;
        public TMPro.TextMeshProUGUI NumpadInputText, NumpadConfirmText, NumpadDetailsText;

        public Color NumpadValidColor, NumpadInvalidColor;

        private int minBet = 0;
        private int chipsToBet = 0;

        private int numpadInput = 0;

        public int BetAmount => chipsToBet;

        public int CallAmount
        {
            get
            {
                int amount = ParentPlayer.Manager.CurGreatestBet;

                if (amount > GetChips())
                    amount = GetChips();
                // Otherwise subtract the amount we've already betted this street so we don't bet more than we need to
                else if (ParentPlayer.Manager.CurStreet > GameManager.InvalidStreet && ParentPlayer.Manager.CurStreet < GameManager.NumStreets)
                    amount -= ParentPlayer.GetBet(ParentPlayer.Manager.CurStreet);

                return amount;
            }
        }

        private int MinimumBet =>
            ParentPlayer.Manager.CurStreet < 0 || ParentPlayer.Manager.CurGreatestBet == 0
            ? GameManager.BigBlind : ParentPlayer.Manager.CurGreatestBet << 1;

        public void SetMinimumBet(int newMinBet)
        {
            minBet = newMinBet;
            Deserialize();
        }
        public void SetBetAmount(int betAmount)
        {
            chipsToBet = betAmount;
            Deserialize();
        }

        public void SetMinimumBetFromGame()
        {
            int amount = MinimumBet;

            if (amount > GetChips())
                amount = GetChips();

            SetMinimumBet(amount);

            if (ParentPlayer.CanCheck)
                SetBetAmount(amount);
            else
                SetCallAmount();

            numpadInput = chipsToBet;
        }

        public void SetCallAmount() => SetBetAmount(CallAmount);

        public override void Deserialize()
        {
            base.Deserialize();

            ParentPlayer.PlayerInfoDisplay.Deserialize();

            bool bettingEnabled = OwnedByLocal && !ParentPlayer.Manager.Processing && ParentPlayer.LocalsTurn && !ParentPlayer.AddtBet.Pickup.IsHeld && !ParentPlayer.DecisionMade && ParentPlayer.Manager.CurStreet != GameManager.ShowdownStreet;

            BetCanvas.SetActive(bettingEnabled && Settings.InputIndex == LocalSettings.SliderInput);
            NumpadCanvas.SetActive(bettingEnabled && Settings.InputIndex == LocalSettings.NumpadInput);
            LimitedActionCanvas.SetActive(false);
            ParentPlayer.AddtActionsCanvas.SetActive(bettingEnabled);

            if (!bettingEnabled)
                return;

            BetText.text = BetEmoji + " " + Settings.FormatCurrency(chipsToBet);
            ParentPlayer.AddtBet.Pickup.InteractionText = BetVerbs[ParentPlayer.LocalSettingsPanel.Settings.LanguageIndex] + Settings.FormatCurrency(chipsToBet);

            // Slider deserialization

            int minVal = 0, maxVal = GetMax();

            if (ParentPlayer.Manager.CurStreet < 0 || ParentPlayer.Manager.CurGreatestBet == 0 || ParentPlayer.CanCheck)
                minVal = minBet;
            else
                minVal = minBet - 1;

            BetSlider.minValue = minVal;
            BetSlider.maxValue = maxVal;

            BetSlider.SetValueWithoutNotify(chipsToBet);

            Debug.Log($"Bankroll of P{ParentPlayer.PlayerNum}: Min = {minVal}, Max = {maxVal}, SMin = {BetSlider.minValue}, SMax = {BetSlider.maxValue}");

            // REMARK: The shit below sucks what was my sleep-deprived ass cooking
            // Also order matters

            // If we have no money, don't let a bet happen
            if (maxVal == 0)
            {
                BetCanvas.SetActive(false);
                LimitedActionCanvas.SetActive(false);
            }
            // If we can only call or go all-in, enforce that
            if (maxVal - minVal == 1)
            {
                if (maxVal == CallAmount)
                {
                    BetCanvas.SetActive(false);
                    LimitedActionCanvas.SetActive(true);
                    CallAndAllInOnly.gameObject.SetActive(false);
                    CallOnly.gameObject.SetActive(false);
                    AllInOnly.gameObject.SetActive(true);
                }
                else
                {
                    BetCanvas.SetActive(false);
                    LimitedActionCanvas.SetActive(Settings.InputIndex == LocalSettings.SliderInput);
                    CallAndAllInOnly.gameObject.SetActive(true);
                    CallOnly.gameObject.SetActive(false);
                    AllInOnly.gameObject.SetActive(false);
                }
            }
            // If we can only call as the highest rolling player, enforce that
            else if (maxVal < minVal)
            {
                if (CallAmount < maxVal)
                {
                    BetCanvas.SetActive(false);
                    LimitedActionCanvas.SetActive(Settings.InputIndex == LocalSettings.SliderInput);
                    CallAndAllInOnly.gameObject.SetActive(true);
                    CallOnly.gameObject.SetActive(false);
                    AllInOnly.gameObject.SetActive(false);
                }
                else
                {
                    BetCanvas.SetActive(false);
                    LimitedActionCanvas.SetActive(true);
                    CallAndAllInOnly.gameObject.SetActive(false);
                    CallOnly.gameObject.SetActive(true);
                    AllInOnly.gameObject.SetActive(false);
                }
            }

            // Numpad deserialization

            if (Settings.InputIndex == LocalSettings.SliderInput)
                numpadInput = chipsToBet;

            string numpadInputString = Settings.FormatCurrency(numpadInput);
            string chipsToBetString = Settings.FormatCurrency(chipsToBet);

            Color validationColor = chipsToBet == numpadInput ? NumpadValidColor : NumpadInvalidColor;
            string validColorString = $"<color=#{(uint)(validationColor.r * 255f):X}{(uint)(validationColor.g * 255f):X}{(uint)(validationColor.b * 255f):X}>";

            NumpadInputText.text = numpadInputString;
            NumpadInputText.color = validationColor;

            NumpadConfirmText.text = chipsToBet == numpadInput ? NumpadValidText : NumpadInvalidText;
            NumpadConfirmText.color = validationColor;

            NumpadDetailsText.text = BuildDetailsString(validColorString, chipsToBetString, chipsToBet == numpadInput);
        }

        public void _OnBetSliderValueChanged()
        {
            int value = (int)BetSlider.value;

            if (!ParentPlayer.CanCheck && value == minBet - 1)
                SetCallAmount();
            else
                SetBetAmount(value);
        }

        private int GetMax()
        {
            int maxTotal = 0, maxBankroll = 0, maxCurBet = 0;

            foreach (Player player in ParentPlayer.Manager.Players)
            {
                if (player == ParentPlayer || !player.HasOwner || player.LateJoiner || ParentPlayer.Manager.PlayerFolded(player.PlayerNum))
                    continue;

                int curBankroll = player.Bankroll.GetChips(), curBet = player.GetBet(ParentPlayer.Manager.CurStreet);

                if (curBankroll + curBet > maxTotal)
                {
                    maxTotal = curBankroll + curBet;
                    maxBankroll = curBankroll;
                    maxCurBet = player.GetBet(ParentPlayer.Manager.CurStreet);
                }
            }

            int bet = ParentPlayer.GetBet(ParentPlayer.Manager.CurStreet);

            if (maxTotal >= GetChips() + bet)
                return GetChips();
            return Mathf.Max(CallAmount, maxBankroll + maxCurBet - bet);
        }

        public bool IsChipLeader()
        {
            int maxTotal = 0;

            foreach (Player player in ParentPlayer.Manager.Players)
            {
                if (player == ParentPlayer || !player.HasOwner || player.LateJoiner || ParentPlayer.Manager.PlayerFolded(player.PlayerNum))
                    continue;

                maxTotal = Mathf.Max(maxTotal, player.Bankroll.GetChips() + player.GetBet(ParentPlayer.Manager.CurStreet));
            }

            return maxTotal < GetChips() + ParentPlayer.GetBet(ParentPlayer.Manager.CurStreet);
        }

        public void _OnIncreaseBy1Pressed() => ++BetSlider.value;

        public void _OnDecreaseBy1Pressed() => --BetSlider.value;

        public void _OnNumpadInput_0() => _AddDigitFromNumpad(0);
        public void _OnNumpadInput_1() => _AddDigitFromNumpad(1);
        public void _OnNumpadInput_2() => _AddDigitFromNumpad(2);
        public void _OnNumpadInput_3() => _AddDigitFromNumpad(3);
        public void _OnNumpadInput_4() => _AddDigitFromNumpad(4);
        public void _OnNumpadInput_5() => _AddDigitFromNumpad(5);
        public void _OnNumpadInput_6() => _AddDigitFromNumpad(6);
        public void _OnNumpadInput_7() => _AddDigitFromNumpad(7);
        public void _OnNumpadInput_8() => _AddDigitFromNumpad(8);
        public void _OnNumpadInput_9() => _AddDigitFromNumpad(9);
        public void _OnNumpadDelete() => _ProcessNumpadInput(numpadInput / 10);
        public void _OnNumpadClear() => _ProcessNumpadInput(0);
        public void _OnNumpadCall() => _ProcessNumpadInput(ParentPlayer.CanCheck ? minBet : CallAmount);
        public void _OnNumpadRaise() => _ProcessNumpadInput(minBet);
        public void _OnNumpadAllIn() => _ProcessNumpadInput(GetMax());

        public void SetInitialRoll() => SetChips(ParentPlayer.Manager.StartingBankrolls);

        public override string GetIndicator() => BankEmoji;

        private void _AddDigitFromNumpad(int digit) => _ProcessNumpadInput(numpadInput * 10 + digit);

        private void _ProcessNumpadInput(int input)
        {
            numpadInput = Mathf.Clamp(input, 0, GetMax());

            if ((!ParentPlayer.CanCheck && numpadInput == CallAmount) || numpadInput >= minBet || (IsChipLeader() && numpadInput == GetMax()))
                chipsToBet = numpadInput;

            Deserialize();
        }

        private string BuildDetailsString(string colorString, string chipsString, bool changed)
        {
            string statusString = string.Empty;

            switch (ParentPlayer.LocalSettingsPanel.Settings.LanguageIndex)
            {
                case Translatable.LangFR:
                    statusString = changed ? "à présent" : "toujours";
                    return $"Le pari est {colorString}{statusString}</color> de {chipsString}. Vous pouvez déplacer votre pari dans cette zone.";

                case Translatable.LangES:
                    statusString = changed ? "es ahora" : "sigue siendo";
                    return $"Su apuesta {colorString}{statusString}</color> de {chipsString}. Puede mover su apuesta a la zona.";

                case Translatable.LangDE:
                    statusString = changed ? "jetzt" : "immer noch";
                    return $"Ihr Einsatz beträgt {colorString}{statusString}</color> {chipsString}. Sie können Ihren Einsatz in den Bereich verschieben.";

                case Translatable.LangJP:
                    statusString = changed ? "になりました" : "のまま";
                    return $"{Translatable.FullWidthLessSquishedAccommodation}ベット額は{chipsString}{colorString}{statusString}</color>。チップをこのエリアに移動させることができます。";
                
                case Translatable.LangKR:
                    statusString = changed ? "현재" : "그대로";
                    return $"{Translatable.FullWidthLessSquishedAccommodation}베트 총액은 {colorString}{statusString}</color> {chipsString}입니다. 베트를 옮길 수 있습니다.";


                case Translatable.LangFI:
                    statusString = changed ? "nyt" : "edelleen";
                    return $"Panos on {colorString}{statusString}</color> {chipsString}. Voit siirtää panoksesi alueen sisään.";

                default:
                    statusString = changed ? "now" : "still";
                    return $"Your bet is {colorString}{statusString}</color> {chipsString}. You can move your bet into the area.";
            }
        }
    }
}
