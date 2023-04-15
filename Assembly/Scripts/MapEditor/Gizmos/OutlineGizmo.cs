using System.Collections.Generic;
using UnityEngine;
using UI;
using Utility;
using CustomSkins;
using ApplicationManagers;
using Map;

namespace MapEditor
{
    class OutlineGizmo : BaseGizmo
    {
        private Dictionary<MapObject, List<GameObject>> _meshOutlines = new Dictionary<MapObject, List<GameObject>>();


        public static OutlineGizmo Create()
        {
            var go = new GameObject();
            var outline = go.AddComponent<OutlineGizmo>();
            return outline;
        }

        public override void OnSelectionChange()
        {
            foreach (MapObject obj in new List<MapObject>(_meshOutlines.Keys))
            {
                if (!_gameManager.SelectedObjects.Contains(obj))
                    DestroyOutline(obj);
            }
            foreach (MapObject obj in _gameManager.SelectedObjects)
            {
                if (!_meshOutlines.ContainsKey(obj))
                    CreateOutline(obj);
            }
        }

        private void CreateOutline(MapObject obj)
        {
            var outlines = new List<GameObject>();
            foreach (MeshFilter filter in obj.GameObject.GetComponentsInChildren<MeshFilter>())
            {
                var outline = new GameObject();
                outline.name = "OutlineGizmo";
                outline.transform.parent = filter.transform;
                outline.transform.localPosition = Vector3.zero;
                outline.transform.localRotation = Quaternion.identity;
                outline.transform.localScale = Vector3.one;
                outline.AddComponent<MeshFilter>();
                outline.AddComponent<MeshRenderer>();
                outline.GetComponent<MeshFilter>().mesh = filter.mesh;
                outline.GetComponent<MeshRenderer>().material = (Material)AssetBundleManager.LoadAsset("OutlineMaterial", true);
                outlines.Add(outline);
            }
            _meshOutlines.Add(obj, outlines);
        }

        private void DestroyOutline(MapObject obj)
        {
            foreach (GameObject go in _meshOutlines[obj])
                Destroy(go);
            _meshOutlines.Remove(obj);
        }
    }
}
