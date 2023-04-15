using System;
using System.Collections.Generic;
using UnityEngine;

namespace Characters
{
    class HumanSetupTextures
    {
        private HumanSetup _setup;

        public HumanSetupTextures(HumanSetup setup)
        {
            _setup = setup;
        }

        public string Get3dmgTexture()
        {
            if (_setup.Weapon == HumanWeapon.Gun)
                return "aottg_hero_AHSS_3dmg";
            return "AOTTG_HERO_3DMG";
        }

        public string GetFaceTexture()
        {
            return "aottg_hero_eyes";
        }

        public string GetBrandTexture()
        {
            switch (_setup.CustomSet.Logo.Value)
            {
                case 0:
                    return "aottg_hero_brand_ts";
                case 1:
                    return "aottg_hero_brand_sc";
                case 2:
                    return "aottg_hero_brand_g";
                case 3:
                    return "aottg_hero_brand_mp";
            }
            return string.Empty;
        }

        public string GetSkinTexture()
        {
            return "aottg_hero_skin_" + (_setup.CustomSet.Skin.Value + 1).ToString();
        }

        public string GetHairTexture()
        {
            return _setup.CurrentHair["Texture"].Value;
        }

        public string GetBodyTexture()
        {
            return _setup.CurrentCostume["Texture"].Value;
        }

        public string GetChestTexture(int chest)
        {
            if (chest == 1)
            {
                if (_setup.CurrentCostume["Type"].ToString().StartsWith("Uniform"))
                    return "aottg_hero_annie_cap_uniform";
                return "aottg_hero_annie_cap_causal";
            }
            return string.Empty;
        }
    }
}
