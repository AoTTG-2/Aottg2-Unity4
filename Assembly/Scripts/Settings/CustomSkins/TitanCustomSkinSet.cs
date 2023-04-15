using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    class TitanCustomSkinSet: BaseSetSetting
    {
        public BoolSetting RandomizedPairs = new BoolSetting(false);
        public ListSetting<StringSetting> Hairs = new ListSetting<StringSetting>(new StringSetting(string.Empty, maxLength: 200), 5);
        public ListSetting<IntSetting> HairModels = new ListSetting<IntSetting>(new IntSetting(0, minValue: 0), 5);
        public ListSetting<StringSetting> Bodies = new ListSetting<StringSetting>(new StringSetting(string.Empty, maxLength: 200), 5);
        public ListSetting<StringSetting> Eyes = new ListSetting<StringSetting>(new StringSetting(string.Empty, maxLength: 200), 5);

        protected override bool Validate()
        {
            return Hairs.Value.Count == 5 && HairModels.Value.Count == 5 && Bodies.Value.Count == 5 && Eyes.Value.Count == 5;
        }
    }
}
