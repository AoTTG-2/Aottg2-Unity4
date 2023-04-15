using Characters;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLogic
{
    class CustomLogicShifterBuiltin : CustomLogicCharacterBuiltin
    {
        public BaseShifter Shifter;

        public CustomLogicShifterBuiltin(BaseShifter shifter) : base(shifter, "Shifter")
        {
            Shifter = shifter;
        }

        public override object CallMethod(string methodName, List<object> parameters)
        {
            return base.CallMethod(methodName, parameters);
        }

        public override object GetField(string name)
        {
            return base.GetField(name);
        }

        public override void SetField(string name, object value)
        {
            base.SetField(name, value);
        }
    }
}
