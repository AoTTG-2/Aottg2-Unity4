using UnityEngine;
using CustomLogic;
using Map;
using Characters;
using System.Collections.Generic;

namespace Map
{
    class CustomLogicCollisionHandler : MonoBehaviour
    {
        List<CustomLogicComponentInstance> _classInstances = new List<CustomLogicComponentInstance>();

        public void RegisterInstance(CustomLogicComponentInstance classInstance)
        {
            _classInstances.Add(classInstance);
        }

        protected void OnCollisionEnter(Collision other)
        {
            var builtin = GetBuiltin(other.collider);
            if (builtin == null)
                return;
            foreach (var classInstance in _classInstances)
                classInstance.OnCollisionEnter(builtin);
        }

        protected void OnCollisionStay(Collision other)
        {
            var builtin = GetBuiltin(other.collider);
            if (builtin == null)
                return;
            foreach (var classInstance in _classInstances)
                classInstance.OnCollisionStay(builtin);
        }

        protected void OnCollisionExit(Collision other)
        {
            var builtin = GetBuiltin(other.collider);
            if (builtin == null)
                return;
            foreach (var classInstance in _classInstances)
                classInstance.OnCollisionExit(builtin);
        }

        protected void OnTriggerEnter(Collider other)
        {
            var builtin = GetBuiltin(other);
            if (builtin == null)
                return;
            foreach (var classInstance in _classInstances)
                classInstance.OnCollisionEnter(builtin);
        }

        protected void OnTriggerStay(Collider other)
        {
            var builtin = GetBuiltin(other);
            if (builtin == null)
                return;
            foreach (var classInstance in _classInstances)
                classInstance.OnCollisionStay(builtin);
        }

        protected void OnTriggerExit(Collider other)
        {
            var builtin = GetBuiltin(other);
            if (builtin == null)
                return;
            foreach (var classInstance in _classInstances)
                classInstance.OnCollisionExit(builtin);
        }

        protected CustomLogicBaseBuiltin GetBuiltin(Collider other)
        {
            var root = other.transform.root;
            var character = root.GetComponent<BaseCharacter>();
            if (character != null)
            {
                CustomLogicBaseBuiltin builtin = null;
                if (character is Human)
                    builtin = new CustomLogicHumanBuiltin((Human)character);
                else if (character is BasicTitan)
                    builtin = new CustomLogicTitanBuiltin((BasicTitan)character);
                else if (character is BaseShifter)
                    builtin = new CustomLogicShifterBuiltin((BaseShifter)character);
                return builtin;
            }
            else if (MapLoader.GoToMapObject.ContainsKey(other.gameObject))
            {
                var mapObject = MapLoader.GoToMapObject[other.gameObject];
                var builtin = new CustomLogicMapObjectBuiltin(mapObject);
                return builtin;
            }
            return null;
        }
    }
}
