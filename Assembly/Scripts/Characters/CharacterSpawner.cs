using ApplicationManagers;
using GameManagers;
using System.Collections.Generic;
using UnityEngine;

namespace Characters
{
    class CharacterSpawner: MonoBehaviour
    {
        public static BaseCharacter Spawn(string name, Vector3 position, Quaternion rotation)
        {
            GameObject go = PhotonNetwork.Instantiate(name, position, rotation, 0);
            BaseCharacter character = null;
            if (name == CharacterPrefabs.Human)
                character = go.GetComponent<Human>();
            else if (name.StartsWith(CharacterPrefabs.BasicTitanPrefix))
                character = go.GetComponent<BasicTitan>();
            else if (name == CharacterPrefabs.Horse)
                character = go.GetComponent<Horse>();
            else if (name == CharacterPrefabs.AnnieShifter)
                character = go.GetComponent<AnnieShifter>();
            else if (name == CharacterPrefabs.ErenShifter)
                character = go.GetComponent<ErenShifter>();
            return character;
        }
    }
}
