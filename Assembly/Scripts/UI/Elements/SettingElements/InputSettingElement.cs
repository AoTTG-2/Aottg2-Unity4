﻿using System;
using UnityEngine.UI;
using UnityEngine;
using Settings;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

namespace UI
{
    class InputSettingElement : BaseSettingElement
    {
        public InputField _inputField;
        protected int _inputFontSizeOffset = -4;
        protected bool _fixedInputField;
        protected UnityAction _onValueChanged;
        protected UnityAction _onEndEdit;
        protected Transform _caret;
        protected bool _finishedSetup;
        protected object[] _setupParams;

        protected override HashSet<SettingType> SupportedSettingTypes => new HashSet<SettingType>()
        {
            SettingType.Float,
            SettingType.Int,
            SettingType.String
        };

        public void Setup(BaseSetting setting, ElementStyle style, string title, string tooltip, float elementWidth, float elementHeight,
            bool multiLine, UnityAction onValueChanged, UnityAction onEndEdit)
        {
            if (style.FontSize <= 18)
                _inputFontSizeOffset = -2;
            _onValueChanged = onValueChanged;
            _onEndEdit = onEndEdit;
            _inputField = transform.Find("InputField").gameObject.GetComponent<InputField>();
            if (_inputField == null)
            {
                _inputField = transform.Find("InputField").gameObject.AddComponent<InputFieldPasteable>();
                _inputField.textComponent = _inputField.transform.Find("Text").GetComponent<Text>();
                _inputField.transition = Selectable.Transition.ColorTint;
                _inputField.targetGraphic = _inputField.GetComponent<Image>();
                // for some reason we need to give it a fake initial input otherwise the graphic won't update
                _inputField.text = "Default";
                _inputField.text = string.Empty;
            }
            _inputField.colors = UIManager.GetThemeColorBlock(style.ThemePanel, "DefaultSetting", "Input");
            _inputField.transform.Find("Text").GetComponent<Text>().color = UIManager.GetThemeColor(style.ThemePanel, "DefaultSetting", "InputTextColor");
            _inputField.selectionColor = UIManager.GetThemeColor(style.ThemePanel, "DefaultSetting", "InputSelectionColor");
            _inputField.transform.Find("Text").GetComponent<Text>().fontSize = style.FontSize + _inputFontSizeOffset;
            _inputField.transform.Find("Text").GetComponent<Text>().color = UIManager.GetThemeColor(style.ThemePanel, "DefaultSetting", "InputTextColor");
            _inputField.GetComponent<LayoutElement>().preferredWidth = elementWidth;
            _inputField.GetComponent<LayoutElement>().preferredHeight = elementHeight;
            _inputField.lineType = multiLine ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            _settingType = GetSettingType(setting);
            if (_settingType == SettingType.Float)
            {
                _inputField.contentType = InputField.ContentType.DecimalNumber;
                _inputField.characterLimit = 20;
            }
            else if (_settingType == SettingType.Int)
            {
                _inputField.contentType = InputField.ContentType.IntegerNumber;
                _inputField.characterLimit = 10;
            }
            else if (_settingType == SettingType.String)
            {
                _inputField.contentType = InputField.ContentType.Standard;
                int maxLength = ((StringSetting)setting).MaxLength;
                if (maxLength == int.MaxValue)
                    _inputField.characterLimit = 0;
                else
                    _inputField.characterLimit = maxLength;
            }
            _inputField.onValueChange.AddListener((string value) => OnValueChanged(value));
            _inputField.onEndEdit.AddListener((string value) => OnInputFinishEditing(value));

            // multiline has a bug when syncing during initialization, so it has to be delayed
            if (multiLine)
            {
                _setupParams = new object[] { setting, style, title, tooltip };
                StartCoroutine(WaitAndFinishSetup());
            }
            else
            {
                base.Setup(setting, style, title, tooltip);
                // Input field needs to be re-activated to show the first set value
                StartCoroutine(WaitAndFixInputField());
                _finishedSetup = true;
            }
        }

        private void OnEnable()
        {
            if (_inputField != null && !_finishedSetup)
            {
                StartCoroutine(WaitAndFinishSetup());
            }
            else if (_inputField != null && !_fixedInputField)
            {
                StartCoroutine(WaitAndFixInputField());
            }
        }

        private IEnumerator WaitAndFinishSetup()
        {
            yield return new WaitForEndOfFrame();
            base.Setup((BaseSetting)_setupParams[0], (ElementStyle)_setupParams[1], (string)_setupParams[2], (string)_setupParams[3]);
            StartCoroutine(WaitAndFixInputField());
            _finishedSetup = true;
        }

        private IEnumerator WaitAndFixInputField()
        {
            yield return new WaitForEndOfFrame();
            _inputField.gameObject.SetActive(false);
            _inputField.gameObject.SetActive(true);
            SyncElement();
            _fixedInputField = true;
        }

        protected void OnValueChanged(string value)
        {
            if (!_finishedSetup)
                return;
            if (_settingType == SettingType.String)
                ((StringSetting)_setting).Value = _inputField.text;
            else if (value != string.Empty)
            {
                if (_settingType == SettingType.Float)
                {
                    float parsedValue;
                    if (float.TryParse(value, out parsedValue))
                        ((FloatSetting)_setting).Value = parsedValue;
                }
                else if (_settingType == SettingType.Int)
                {
                    int parsedValue;
                    if (int.TryParse(value, out parsedValue))
                        ((IntSetting)_setting).Value = parsedValue;
                }
            }
            if (_onValueChanged != null)
                _onValueChanged.Invoke();
        }

        protected void OnInputFinishEditing(string value)
        {
            if (!_finishedSetup)
                return;
            SyncElement();
            if (_onEndEdit != null)
                _onEndEdit.Invoke();
        }

        public override void SyncElement()
        {
            if (!_finishedSetup)
                return;
            if (_settingType == SettingType.Float)
                _inputField.text = ((FloatSetting)_setting).Value.ToString();
            else if (_settingType == SettingType.Int)
                _inputField.text = ((IntSetting)_setting).Value.ToString();
            else if (_settingType == SettingType.String)
                _inputField.text = ((StringSetting)_setting).Value;
            _inputField.transform.Find("Text").GetComponent<Text>().text = _inputField.text;
        }

        private void Update()
        {
            if (!_caret && _inputField != null)
            {
                _caret = _inputField.transform.Find(_inputField.transform.name + " Input Caret");
                if (_caret)
                {
                    var graphic = _caret.GetComponent<Graphic>();
                    if (!graphic)
                        _caret.gameObject.AddComponent<Image>();
                }
            }
            if (_inputField.isFocused)
            {
                if ((_inputField.contentType == InputField.ContentType.DecimalNumber || _inputField.contentType == InputField.ContentType.IntegerNumber) &&
                    (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)))
                {
                    _inputField.text = "-";
                }
            }
        }
    }

    public class InputFieldPasteable : InputField
    {
        protected bool IsModifier()
        {
            if (Application.platform == RuntimePlatform.OSXPlayer)
                return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        protected bool IsCopy()
        {
            return Input.GetKeyDown(KeyCode.C);
        }

        protected bool IsPaste()
        {
            return Input.GetKeyDown(KeyCode.V);
        }

        protected bool IsCut()
        {
            return Input.GetKeyDown(KeyCode.X);
        }

        protected override void Append(char input)
        {
            if (Application.platform == RuntimePlatform.OSXPlayer && IsModifier())
            {
                if (IsCopy() || IsCut() || IsPaste())
                {
                    return;
                }
            }
            base.Append(input);
        }

        protected override void Append(string input)
        {
            if (Application.platform == RuntimePlatform.OSXPlayer && IsModifier())
            {
                if (IsCopy() || IsCut())
                    return;
            }
            if (multiLine)
            {
                if (IsModifier() && IsPaste())
                {
                    input = GetClipboard();
                    int insert = caretPosition;
                    if (caretPosition != m_CaretSelectPosition && text.Length > 0)
                    {
                        int start = Math.Min(caretPosition, m_CaretSelectPosition);
                        int end = Math.Max(caretPosition, m_CaretSelectPosition);
                        if (end >= text.Length)
                            text = text.Substring(0, start);
                        else
                            text = text.Substring(0, start) + text.Substring(end, text.Length - end);
                        insert = start;
                    }
                    if (insert >= text.Length || text.Length == 0)
                        text = text + input;
                    else
                        text = text.Substring(0, insert) + input + text.Substring(insert, text.Length - insert);
                    onValueChange.Invoke(text);
                    m_CaretSelectPosition = caretPosition = Math.Min(insert + input.Length, text.Length);
                    return;
                }
            }
            if (!multiLine && Application.platform == RuntimePlatform.OSXPlayer && IsModifier() && IsPaste())
            {
                foreach (char c in GetClipboard())
                    base.Append(c);
                return;
            }
            base.Append(input);
        }

        private string GetClipboard()
        {
            TextEditor editor = new TextEditor();
            editor.multiline = multiLine;
            editor.Paste();
            return editor.content.text;
        }
    }
}
