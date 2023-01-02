using BepInEx.Configuration;
using System;
using UnityEngine;

namespace AlbaVR
{
    public class AcceptableKeyCodeValueList : AcceptableValueBase
    {
        public AcceptableKeyCodeValueList() : base(typeof(KeyCode))
        {
        }

        public override object Clamp(object value)
        {
            if (IsValid(value))
            {
                return value;
            }
            else
            {
                return KeyCode.None;
            }
        }

        public override bool IsValid(object value)
        {
            return Enum.IsDefined(typeof(KeyCode), value);
        }

        public override string ToDescriptionString()
        {
            return
                "# Refer to this page for a list of allowed values:\n" +
                "# https://docs.unity3d.com/2019.4/Documentation/ScriptReference/KeyCode.html";
        }
    }
}
