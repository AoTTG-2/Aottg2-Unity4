using Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UI
{
    class MainMenuPopup: BasePopup
    {
        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
        }

        public override void Hide()
        {
            if (!IsActive)
                return;
            ((MainMenu)UIManager.CurrentMenu).PlaySound("Back");
            base.Hide();
        }
    }
}
