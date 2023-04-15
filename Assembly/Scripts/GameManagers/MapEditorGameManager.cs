using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Utility;
using CustomSkins;
using ApplicationManagers;
using Map;
using Settings;
using MapEditor;
using CustomLogic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameManagers
{
    class MapEditorGameManager : BaseGameManager
    {
        public MapScript MapScript;
        public CustomLogicEvaluator LogicEvaluator;
        public HashSet<MapObject> SelectedObjects = new HashSet<MapObject>();
        private List<BaseCommand> _undoCommands = new List<BaseCommand>();
        private List<BaseCommand> _redoCommands = new List<BaseCommand>();
        private string _clipboard = string.Empty;
        private MapEditorMenu _menu;
        private MapEditorInputSettings _input;
        private PositionGizmo _positionGizmo;
        private OutlineGizmo _outlineGizmo;
        private int _currentObjectId;
        public bool IgnoreNextSelect;
        
        public void ShowAddObject()
        {
            if (_menu.AddObjectPopup.IsActive)
                _menu.AddObjectPopup.Hide();
            else if (!_menu.IsMouseUI)
                _menu.AddObjectPopup.Show();
        }

        public void AddObject(string name)
        {
            var mapScriptObjects = new MapScriptObjects();
            var prefab = BuiltinMapPrefabs.AllPrefabs[name];
            prefab.SetPosition(SceneLoader.CurrentCamera.Cache.Transform.position + SceneLoader.CurrentCamera.Cache.Transform.forward * 50f);
            mapScriptObjects.Objects.Add(prefab);
            NewCommand(new AddObjectCommand(mapScriptObjects.Objects));
            DeselectAll();
            SelectObject(MapLoader.IdToMapObject[_currentObjectId]);
            _menu.SyncHierarchyPanel();
            OnSelectionChange();
        }

        public void Undo()
        {
            if (_undoCommands.Count == 0)
                return;
            var command = _undoCommands[_undoCommands.Count - 1];
            command.Unexecute();
            _redoCommands.Add(command);
            _undoCommands.RemoveAt(_undoCommands.Count - 1);
            if (command is AddObjectCommand || command is DeleteObjectCommand)
                _menu.SyncHierarchyPanel();
            OnSelectionChange();
        }

        public void Redo()
        {
            if (_redoCommands.Count == 0)
                return;
            var command = _redoCommands[_redoCommands.Count - 1];
            command.Execute();
            _undoCommands.Add(command);
            _redoCommands.RemoveAt(_redoCommands.Count - 1);
            if (command is AddObjectCommand || command is DeleteObjectCommand)
                _menu.SyncHierarchyPanel();
            OnSelectionChange();
        }

        public void Copy()
        {
            if (SelectedObjects.Count == 0)
                return;
            var mapScriptObjects = new MapScriptObjects();
            foreach (var obj in SelectedObjects)
                mapScriptObjects.Objects.Add(obj.ScriptObject);
            _clipboard = mapScriptObjects.Serialize();
        }

        public void Paste()
        {
            if (_clipboard == string.Empty)
                return;
            var mapScriptObjects = new MapScriptObjects();
            mapScriptObjects.Deserialize(_clipboard);
            NewCommand(new AddObjectCommand(mapScriptObjects.Objects));
            DeselectAll();
            foreach (var obj in mapScriptObjects.Objects)
                SelectObject(MapLoader.IdToMapObject[obj.Id]);
            _menu.SyncHierarchyPanel();
            OnSelectionChange();
        }

        public void Cut()
        {
            Copy();
            Delete();
        }

        public void Delete()
        {
            if (SelectedObjects.Count == 0)
                return;
            NewCommand(new DeleteObjectCommand(new List<MapObject>(SelectedObjects)));
            _menu.SyncHierarchyPanel();
            OnSelectionChange();
        }

        public void Select(bool multi)
        {
            var camera = SceneLoader.CurrentCamera;
            RaycastHit hit;
            if (Physics.Raycast(camera.Camera.ScreenPointToRay(Input.mousePosition), out hit, 100000f, PhysicsLayer.GetMask(PhysicsLayer.MapEditorGizmo)))
            {
            }
            else if (Physics.Raycast(camera.Camera.ScreenPointToRay(Input.mousePosition), out hit, 100000f, PhysicsLayer.GetMask(PhysicsLayer.MapEditorObject)))
            {
                var mapObject = MapLoader.FindObjectFromCollider(hit.collider);
                if (multi)
                {
                    if (SelectedObjects.Contains(mapObject))
                        DeselectObject(mapObject);
                    else
                        SelectObject(mapObject);
                }
                else
                {
                    if (SelectedObjects.Count == 1 && SelectedObjects.Contains(mapObject))
                        DeselectAll();
                    else if (SelectedObjects.Count > 0)
                    {
                        DeselectAll();
                        SelectObject(mapObject);
                    }
                    else
                        SelectObject(mapObject);
                }
                OnSelectionChange();
            }
            else
            {
                DeselectAll();
                OnSelectionChange();
            }
        }

        public void DeselectAll()
        {
            foreach (MapObject obj in new List<MapObject>(SelectedObjects))
                DeselectObject(obj);
        }

        public void DeselectObject(MapObject obj)
        {
            SelectedObjects.Remove(obj);
        }

        public void SelectObject(MapObject obj)
        {
            SelectedObjects.Add(obj);
        }

        public void NewCommand(BaseCommand command)
        {
            command.Execute();
            _undoCommands.Add(command);
            _redoCommands.Clear();
            if (command is TransformPositionCommand)
                _menu.SyncInspector();
        }

        protected override void OnFinishLoading()
        {
            MapScript = MapManager.MapScript;
            _menu = (MapEditorMenu)UIManager.CurrentMenu;
            _positionGizmo = PositionGizmo.Create();
            _outlineGizmo = OutlineGizmo.Create();
            _currentObjectId = GetHighestObjectId();
            _menu.ShowHierarchyPanel();
            LogicEvaluator = CustomLogicManager.GetEditorEvaluator(MapScript.Logic);
        }

        protected override void Awake()
        {
            base.Awake();
            _input = SettingsManager.InputSettings.MapEditor;
        }

        protected void Update()
        {
            UpdateInput();
        }

        protected void UpdateInput()
        {
            if (_menu == null)
                return;
            if (_input.SaveMap.GetKeyDown())
            {
                Save();
                return;
            }
            if (_menu.IsPopupActive())
                return;
            var system = EventSystem.current;
            var selected = system.currentSelectedGameObject;
            if (selected != null && selected.GetComponent<InputField>() != null)
                return;
            if (_input.Undo.GetKeyDown())
                Undo();
            else if (_input.Redo.GetKeyDown())
                Redo();
            else if (_input.CopyObjects.GetKeyDown())
                Copy();
            else if (_input.Paste.GetKeyDown())
                Paste();
            else if (_input.Cut.GetKeyDown())
                Cut();
            else if (_input.AddObject.GetKeyDown())
                ShowAddObject();
            else if (_input.Delete.GetKeyDown())
                Delete();
            else if (_input.Select.GetKeyUp() && !_menu.IsMouseUI && !IgnoreNextSelect)
                Select(_input.Multiselect.GetKey() && SelectedObjects.Count > 0);
            else if (_input.Deselect.GetKeyDown())
            {
                DeselectAll();
                OnSelectionChange();
            }
            
            IgnoreNextSelect = false;
        }

        public void Save()
        {
            _menu._topPanel.Save();
        }

        public void OnSelectionChange()
        {
            foreach (var obj in new HashSet<MapObject>(SelectedObjects))
            {
                if (!MapLoader.IdToMapObject.ContainsKey(obj.ScriptObject.Id))
                    DeselectObject(obj);
            }
            if (SelectedObjects.Count == 1)
                _menu.ShowInspector(new List<MapObject>(SelectedObjects)[0]);
            else
                _menu.HideInspector();
            _menu.HierarchyPanel.SyncSelectedItems();
            SyncGizmos();
        }

        public void SyncGizmos()
        {
            _outlineGizmo.OnSelectionChange();
            _positionGizmo.OnSelectionChange();
        }

        public int GetNextObjectId()
        {
            _currentObjectId++;
            return _currentObjectId;
        }

        protected int GetHighestObjectId()
        {
            int max = 0;
            foreach (int id in MapLoader.IdToMapObject.Keys)
            {
                max = Mathf.Max(max, id);
            }
            return max;
        }
    }
}
