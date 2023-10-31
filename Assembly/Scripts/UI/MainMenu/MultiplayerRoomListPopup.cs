﻿using GameManagers;
using Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    class MultiplayerRoomListPopup: BasePopup
    {
        protected override string ThemePanel => "MultiplayerRoomListPopup";
        protected override bool HasPremadeContent => true;
        protected override int HorizontalPadding => 0;
        protected override int VerticalPadding => 0;
        protected override float Width => 1000f;
        protected override float Height => 660f;
        protected MultiplayerPasswordPopup _multiplayerPasswordPopup;
        protected MultiplayerFilterPopup _multiplayerFilterPopup;
        protected Text _pageLabel;
        protected Text _playersOnlineLabel;
        protected GameObject _roomList;
        protected GameObject _noRoomsLabel;
        protected List<GameObject> _roomButtons = new List<GameObject>();
        public StringSetting _filterQuery = new StringSetting(string.Empty);
        public BoolSetting _filterShowFull = new BoolSetting(true);
        public BoolSetting _filterShowPassword = new BoolSetting(true);
        protected IntSetting _currentPage = new IntSetting(0, minValue: 0);
        private float _maxUpdateDelay = 5f;
        private float _currentUpdateDelay = 5f;
        private int _roomsPerPage = 10;
        private RoomInfo[] _rooms;
        private char[] _roomSeperator = new char[] { "`"[0] };
        private int _lastPageCount = 0;

        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            string cat = "MainMenu";
            string sub = "MultiplayerRoomListPopup";
            ElementStyle buttonStyle = new ElementStyle(fontSize: ButtonFontSize, themePanel: ThemePanel);
            ElementFactory.CreateTextButton(BottomBar, buttonStyle, UIManager.GetLocaleCommon("Create"),
                onClick: () => OnButtonClick("Create"));
            ElementFactory.CreateTextButton(BottomBar, buttonStyle, UIManager.GetLocaleCommon("Back"),
                onClick: () => OnButtonClick("Back"));
            InputSettingElement element = TopBar.Find("SearchInputSetting").gameObject.AddComponent<InputSettingElement>();
            element.Setup(_filterQuery, new ElementStyle(titleWidth: 0f), UIManager.GetLocaleCommon("Search"), string.Empty, 160f, 40f, false, null,
                () => RefreshList());
            TopBar.Find("FilterButton").GetComponent<Button>().onClick.AddListener(() => OnButtonClick("Filter"));
            TopBar.Find("RefreshButton").GetComponent<Button>().onClick.AddListener(() => OnButtonClick("Refresh"));
            TopBar.Find("Page/LeftButton").GetComponent<Button>().onClick.AddListener(() => OnButtonClick("LeftPage"));
            TopBar.Find("Page/RightButton").GetComponent<Button>().onClick.AddListener(() => OnButtonClick("RightPage"));
            _pageLabel = TopBar.Find("Page/PageLabel").GetComponent<Text>();
            _roomList = SinglePanel.Find("RoomList").gameObject;
            _noRoomsLabel = _roomList.transform.Find("NoRoomsLabel").gameObject;
            _noRoomsLabel.GetComponent<Text>().text = UIManager.GetLocale(cat, sub, "NoRooms");
            _playersOnlineLabel = TopBar.Find("PlayersOnlineLabel").GetComponent<Text>();
            _playersOnlineLabel.text = "0 " + UIManager.GetLocale(cat, sub, "PlayersOnline");
            TopBar.Find("FilterButton").Find("Text").GetComponent<Text>().text = UIManager.GetLocaleCommon("Filters");
            foreach (Button button in TopBar.GetComponentsInChildren<Button>())
            {
                button.colors = UIManager.GetThemeColorBlock(buttonStyle.ThemePanel, "DefaultButton", "");
                if (button.transform.Find("Text") != null)
                    button.transform.Find("Text").GetComponent<Text>().color = UIManager.GetThemeColor(buttonStyle.ThemePanel, "DefaultButton", "TextColor");
            }
            TopBar.Find("Page/PageLabel").GetComponent<Text>().color = UIManager.GetThemeColor(buttonStyle.ThemePanel, "DefaultLabel", "TextColor");
            TopBar.Find("PlayersOnlineLabel").GetComponent<Text>().color = UIManager.GetThemeColor(buttonStyle.ThemePanel, "DefaultLabel", "TextColor");
            _noRoomsLabel.GetComponent<Text>().color = UIManager.GetThemeColor(buttonStyle.ThemePanel, "RoomButton", "TextColor");
            _roomList.GetComponent<RawImage>().texture = UIManager.GetThemeTexture(buttonStyle.ThemePanel, "MainBody", "BackgroundTexture");
            _roomList.GetComponent<RawImage>().color = UIManager.GetThemeColor(buttonStyle.ThemePanel, "MainBody", "BackgroundColor");
        }

        public override void Show()
        {
            base.Show();
            _currentPage.Value = 0;
            RefreshList();
            _currentUpdateDelay = 0.5f;
        }

        public override void Hide()
        {
            if (gameObject.activeSelf)
                PhotonNetwork.Disconnect();
            base.Hide();
        }

        public void HideNoDisconnect()
        {
            base.Hide();
        }

        protected void Update()
        {
            _currentUpdateDelay -= Time.deltaTime;
            if (_currentUpdateDelay <= 0f)
            {
                RefreshList();
                _currentUpdateDelay = _maxUpdateDelay;
            }
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();
            _multiplayerPasswordPopup = ElementFactory.CreateHeadedPanel<MultiplayerPasswordPopup>(transform).GetComponent<MultiplayerPasswordPopup>();
            _multiplayerFilterPopup = ElementFactory.CreateHeadedPanel<MultiplayerFilterPopup>(transform).GetComponent<MultiplayerFilterPopup>();
            _popups.Add(_multiplayerPasswordPopup);
            _popups.Add(_multiplayerFilterPopup);
        }

        public void RefreshList(bool refetch = true)
        {
            _currentUpdateDelay = _maxUpdateDelay;
            if (refetch)
            {
                _rooms = PhotonNetwork.GetRoomList();
                _playersOnlineLabel.text = PhotonNetwork.countOfPlayers + " " + 
                    UIManager.GetLocale("MainMenu", "MultiplayerRoomListPopup", "PlayersOnline");
            }
            ClearRoomButtons();
            List<RoomInfo> filteredRooms = GetFilteredRooms();
            if (filteredRooms.Count == 0)
            {
                _noRoomsLabel.SetActive(true);
                _pageLabel.text = "0/0";
                return;
            }
            _noRoomsLabel.SetActive(false);
            _lastPageCount = GetPageCount(filteredRooms);
            _currentPage.Value = Math.Min(_currentPage.Value, _lastPageCount - 1);
            _pageLabel.text = (_currentPage.Value + 1) + "/" + _lastPageCount;
            List<RoomInfo> pageRooms = GetCurrentPageRooms(filteredRooms);
            foreach (RoomInfo room in pageRooms)
            {
                GameObject button = ElementFactory.InstantiateAndBind(_roomList.transform, "MultiplayerRoomButton");
                _roomButtons.Add(button);
                button.GetComponent<Button>().onClick.AddListener(() => OnRoomClick(room));
                button.transform.Find("Text").GetComponent<Text>().text = GetRoomFormattedName(room);
                if (GetRoomPassword(room) == string.Empty)
                {
                    button.transform.Find("PasswordIcon").gameObject.SetActive(false);
                }
                button.GetComponent<Button>().colors = UIManager.GetThemeColorBlock(ThemePanel, "RoomButton", "");
                button.transform.Find("Text").GetComponent<Text>().color = UIManager.GetThemeColor(ThemePanel, "RoomButton", "TextColor");
            }
        }

        protected List<RoomInfo> GetCurrentPageRooms(List<RoomInfo> rooms)
        {
            if (rooms.Count <= _roomsPerPage)
                return rooms;
            List<RoomInfo> pageRooms = new List<RoomInfo>();
            int startIndex = _currentPage.Value * _roomsPerPage;
            int endIndex = Math.Min(startIndex + _roomsPerPage, rooms.Count);
            for (int i = startIndex; i < endIndex; i++)
            {
                pageRooms.Add(rooms[i]);
            }
            return pageRooms;
        }

        protected List<RoomInfo> GetFilteredRooms()
        {
            List<RoomInfo> filteredRooms = new List<RoomInfo>();
            foreach (RoomInfo room in _rooms)
            {
                string name = room.GetStringProperty(RoomProperty.Name);
                if (!IsValidRoom(room))
                    continue;
                if (_filterQuery.Value != string.Empty && !name.ToLower().Contains(_filterQuery.Value.ToLower()))
                    continue;
                if (!_filterShowFull.Value && room.playerCount >= room.maxPlayers)
                    continue;
                if (!_filterShowPassword.Value && GetRoomPassword(room) != string.Empty)
                    continue;
                filteredRooms.Add(room);
            }
            return filteredRooms;
        }

        protected int GetPageCount(List<RoomInfo> rooms)
        {
            if (rooms.Count == 0)
                return 0;
            return ((rooms.Count - 1) / _roomsPerPage) + 1;
        }

        protected void ClearRoomButtons()
        {
            foreach (GameObject obj in _roomButtons)
            {
                Destroy(obj);
            }
            _roomButtons.Clear();
        }

        protected bool IsValidRoom(RoomInfo info)
        {
            return info.customProperties.ContainsKey(RoomProperty.Name) && info.customProperties.ContainsKey(RoomProperty.Map)
                && info.customProperties.ContainsKey(RoomProperty.GameMode) && info.customProperties.ContainsKey(RoomProperty.Password);
        }

        protected string GetRoomPassword(RoomInfo info)
        {
            return info.GetStringProperty(RoomProperty.Password);
        }

        protected string GetRoomFormattedName(RoomInfo room)
        {
            string name = room.GetStringProperty(RoomProperty.Name).HexColor();
            string map = room.GetStringProperty(RoomProperty.Map);
            string mode = room.GetStringProperty(RoomProperty.GameMode);
            object[] objArray1 = new object[] { name, " / ", map, " / ", mode, "   ", room.playerCount, "/", room.maxPlayers };
            return string.Concat(objArray1);
        }

        private void OnRoomClick(RoomInfo room)
        {
            string password = GetRoomPassword(room);
            if (password != string.Empty)
            {
                HideAllPopups();
                _multiplayerPasswordPopup.Show(password, room.name);
            }
            else
            {
                PhotonNetwork.JoinRoom(room.name);
            }
        }

        private void OnButtonClick(string name)
        {
            HideAllPopups();
            switch (name)
            {
                case "Back":
                    ((MainMenu)UIManager.CurrentMenu).ShowMultiplayerMapPopup();
                    break;
                case "Create":
                    HideNoDisconnect();
                    ((CreateGamePopup)((MainMenu)UIManager.CurrentMenu)._createGamePopup).Show(true);
                    break;
                case "Filter":
                    _multiplayerFilterPopup.Show();
                    break;
                case "Refresh":
                    RefreshList();
                    break;
                case "LeftPage":
                    if (_currentPage.Value <= 0)
                        _currentPage.Value = _lastPageCount - 1;
                    else
                        _currentPage.Value -= 1;
                    RefreshList(false);
                    break;
                case "RightPage":
                    if (_currentPage.Value >= _lastPageCount - 1)
                        _currentPage.Value = 0;
                    else
                        _currentPage.Value += 1;
                    RefreshList(false);
                    break;
            }
        }
    }
}
