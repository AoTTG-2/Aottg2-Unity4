using ApplicationManagers;
using Settings;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace CustomLogic
{
    class CustomLogicInputBuiltin: CustomLogicBaseBuiltin
    {
        public CustomLogicInputBuiltin(): base("Input")
        {
        }

        public override object CallMethod(string name, List<object> parameters)
        {
            if (name == "GetKeyHold" || name == "GetKeyDown" || name == "GetKeyUp")
            {
                string key = (string)parameters[0];
                string[] strArr = key.Split('/');
                KeybindSetting keybind = (KeybindSetting)((SaveableSettingsContainer)SettingsManager.InputSettings.Settings[strArr[0]]).Settings[strArr[1]];
                if (name == "GetKeyDown")
                    return keybind.GetKeyDown();
                if (name == "GetKeyUp")
                    return keybind.GetKeyUp();
                if (name == "GetKeyHold")
                    return keybind.GetKey();
            }
            else if (name == "GetKeyName")
            {
                string key = (string)parameters[0];
                string[] strArr = key.Split('/');
                KeybindSetting keybind = (KeybindSetting)((SaveableSettingsContainer)SettingsManager.InputSettings.Settings[strArr[0]]).Settings[strArr[1]];
                return keybind.ToString();
            }
            return base.CallMethod(name, parameters);
        }
    }
}
