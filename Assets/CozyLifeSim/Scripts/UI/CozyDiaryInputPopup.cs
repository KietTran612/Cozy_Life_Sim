using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    public class CozyDiaryInputPopup : CozyPopup
    {
        private const int MaxCharacters = 60;

        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action<string> _onConfirmed;

        protected override void Start()
        {
            base.Start();

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(Confirm);
            }
            if (_cancelButton != null && _cancelButton != _closeButton)
            {
                _cancelButton.onClick.AddListener(Close);
            }
            if (_inputField != null)
            {
                _inputField.characterLimit = MaxCharacters;
            }
        }

        public void OpenPopup(Action<string> onConfirmed)
        {
            _onConfirmed = onConfirmed;
            if (_inputField != null)
            {
                _inputField.text = string.Empty;
                _inputField.characterLimit = MaxCharacters;
            }
            Open();
            if (_inputField != null)
            {
                _inputField.ActivateInputField();
            }
        }

        private void Confirm()
        {
            string value = _inputField != null ? _inputField.text : string.Empty;
            value = Normalize(value);
            if (!string.IsNullOrEmpty(value))
            {
                _onConfirmed?.Invoke(value);
            }
            Close();
        }

        public override void Close()
        {
            base.Close();
            _onConfirmed = null;
        }

        protected override void OnDestroy()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(Confirm);
            }
            if (_cancelButton != null && _cancelButton != _closeButton)
            {
                _cancelButton.onClick.RemoveListener(Close);
            }
            _onConfirmed = null;
            base.OnDestroy();
        }

        private static string Normalize(string value)
        {
            value = value == null ? string.Empty : value.Trim();
            return value.Length <= MaxCharacters ? value : value.Substring(0, MaxCharacters);
        }
    }
}
