using Characters;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLogic
{
    abstract class CustomLogicCharacterBuiltin: CustomLogicBaseBuiltin
    {
        public BaseCharacter Character;
        public CustomLogicCharacterBuiltin(BaseCharacter character, string type = "Character"): base(type)
        {
            Character = character;
        }

        public override object CallMethod(string name, List<object> parameters)
        {
            if (name == "GetKilled")
            {
                string killer = (string)parameters[0];
                Character.GetKilled(killer);
                return null;
            }
            else if (name == "GetDamaged")
            {
                string killer = (string)parameters[0];
                int damage = parameters[1].UnboxToInt();
                Character.GetDamaged(killer, damage);
                return null;
            }
            else if (name == "Emote")
            {
                string emote = (string)parameters[0];
                if (Character.IsMine() && !Character.Dead)
                    Character.Emote(emote);
            }
            return base.CallMethod(name, parameters);
        }

        public override object GetField(string name)
        {
            if (name == "Player")
                return new CustomLogicPlayerBuiltin(Character.Cache.PhotonView.owner);
            else if (name == "IsMine")
                return Character.IsMine();
            else if (name == "Name")
                return Character.Name;
            else if (name == "IsMainCharacter")
                return Character.IsMainCharacter();
            else if (name == "Position")
                return new CustomLogicVector3Builtin(Character.Cache.Transform.position);
            else if (name == "Rotation")
                return new CustomLogicVector3Builtin(Character.Cache.Transform.rotation.eulerAngles);
            else if (name == "Velocity")
                return new CustomLogicVector3Builtin(Character.Cache.Rigidbody.velocity);
            else if (name == "Team")
                return Character.Team;
            else if (name == "IsCharacter")
                return true;
            else if (name == "Health")
                return Character.CurrentHealth;
            else if (name == "MaxHealth")
                return Character.MaxHealth;
            return base.GetField(name);
        }

        public override void SetField(string name, object value)
        {
            if (!Character.IsMine())
                return;
            if (name == "Position")
                Character.Cache.Transform.position = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "Rotation")
                Character.Cache.Transform.rotation = Quaternion.Euler(((CustomLogicVector3Builtin)value).Value);
            else if (name == "Velocity")
                Character.Cache.Rigidbody.velocity = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "Health")
                Character.SetCurrentHealth(value.UnboxToInt());
            else if (name == "MaxHealth")
                Character.SetMaxHealth(value.UnboxToInt());
            else
                base.SetField(name, value);
        }

        public override bool Equals(object other)
        {
            if (other == null)
                return Character == null;
            if (!(other is CustomLogicCharacterBuiltin))
                return false;
            return Character == ((CustomLogicCharacterBuiltin)other).Character;
        }
    }
}
