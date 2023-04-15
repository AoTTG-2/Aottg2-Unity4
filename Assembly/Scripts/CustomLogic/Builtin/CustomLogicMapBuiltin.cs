using ApplicationManagers;
using GameManagers;
using Map;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLogic
{
    class CustomLogicMapBuiltin: CustomLogicBaseBuiltin
    {
        public CustomLogicMapBuiltin(): base("Map")
        {
        }

        public override object CallMethod(string name, List<object> parameters)
        {
            if (name == "FindMapObjectByName")
            {
                string objectName = (string)parameters[0];
                foreach (MapObject mapObject in MapLoader.GoToMapObject.Values)
                {
                    if (mapObject.ScriptObject.Name == objectName)
                        return new CustomLogicMapObjectBuiltin(mapObject);
                }
                return null;
            }
            else if (name == "FindMapObjectsByName")
            {
                CustomLogicListBuiltin listBuiltin = new CustomLogicListBuiltin();
                string objectName = (string)parameters[0];
                foreach (MapObject mapObject in MapLoader.GoToMapObject.Values)
                {
                    if (mapObject.ScriptObject.Name == objectName)
                        listBuiltin.List.Add(new CustomLogicMapObjectBuiltin(mapObject));
                }
                return listBuiltin;
            }
            else if (name == "FindMapObjectByTag")
            {
                string tag = (string)parameters[0];
                if (MapLoader.Tags.ContainsKey(tag))
                {
                    if (MapLoader.Tags[tag].Count > 0)
                        return new CustomLogicMapObjectBuiltin(MapLoader.Tags[tag][0]);
                }
                return null;
            }
            else if (name == "FindMapObjectsByTag")
            {
                CustomLogicListBuiltin listBuiltin = new CustomLogicListBuiltin();
                string tag = (string)parameters[0];
                if (MapLoader.Tags.ContainsKey(tag))
                {
                    {
                        foreach (MapObject mapObject in MapLoader.Tags[tag])
                            listBuiltin.List.Add(new CustomLogicMapObjectBuiltin(mapObject));
                    }
                }
                return listBuiltin;
            }
            else if (name == "CreateMapObjectRaw")
            {
                string prefab = (string)parameters[0];
                prefab = string.Join("", prefab.Split('\n'));
                var script = new MapScriptSceneObject();
                script.Deserialize(prefab);
                script.Id = MapLoader.GetNextObjectId();
                script.Parent = 0;
                var mapObject = MapLoader.LoadObject(script, false);
                MapLoader.SetParent(mapObject);
                return new CustomLogicMapObjectBuiltin(mapObject);
            }
            else if (name == "DestroyMapObject")
            {
                var mapObject = (CustomLogicMapObjectBuiltin)parameters[0];
                MapLoader.DeleteObject(mapObject.Value);
            }
            else if (name == "CopyMapObject")
            {
                var mapObject = (CustomLogicMapObjectBuiltin)parameters[0];
                var script = new MapScriptSceneObject();
                script.Deserialize(mapObject.Value.ScriptObject.Serialize());
                script.Id = MapLoader.GetNextObjectId();
                var copy = MapLoader.LoadObject(script, false);
                MapLoader.SetParent(copy);
                return new CustomLogicMapObjectBuiltin(copy);
            }
            return base.CallMethod(name, parameters);
        }

        public override object GetField(string name)
        {
            return base.GetField(name);
        }

        public override void SetField(string name, object value)
        {
        }
    }
}
