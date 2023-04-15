using ApplicationManagers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    class SimplePanel : BasePanel
    {
        public override void Setup(BasePanel parent = null)
        {
            transform.Find("Background").GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "BackgroundColor");
            base.Setup(parent);
        }
    }
}
