using Settings;
using UnityEngine.UI;

namespace UI
{
    class EditProfileProfilePanel: CategoryPanel
    {
        protected override bool ScrollBar => true;
        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            ProfileSettings settings = SettingsManager.ProfileSettings;
            ElementStyle style = new ElementStyle(titleWidth: 100f, themePanel: ThemePanel);
            var group = ElementFactory.CreateHorizontalGroup(SinglePanel, 40f, UnityEngine.TextAnchor.MiddleLeft).transform;
            ElementFactory.CreateDropdownSetting(group, style, settings.ProfileIcon, UIManager.GetLocaleCommon("Icon"),
                UIManager.AvailableProfileIcons.ToArray(), elementWidth: 260f, onDropdownOptionSelect: () => Parent.RebuildCategoryPanel());
            ElementFactory.CreateRawImage(group, style, UIManager.GetProfileIcon(settings.ProfileIcon.Value), 256, 256);
            CreateHorizontalDivider(SinglePanel);
            ElementFactory.CreateInputSetting(SinglePanel, style, settings.Name, UIManager.GetLocaleCommon("Name"), elementWidth: 260f);
            ElementFactory.CreateInputSetting(SinglePanel, style, settings.Guild, UIManager.GetLocaleCommon("Guild"), elementWidth: 260f);
            ElementFactory.CreateInputSetting(SinglePanel, style, settings.Social, UIManager.GetLocaleCommon("Social"), elementWidth: 260f);
            ElementFactory.CreateInputSetting(SinglePanel, style, settings.About, UIManager.GetLocaleCommon("About"), elementWidth: 260f, elementHeight: 120f, 
                multiLine: true);
        }
    }
}
