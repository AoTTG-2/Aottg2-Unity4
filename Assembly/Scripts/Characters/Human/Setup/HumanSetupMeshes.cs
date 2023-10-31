using Settings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Characters
{
    class HumanSetupMeshes
    {
        private HumanSetup _setup;

        public HumanSetupMeshes(HumanSetup setup)
        {
            _setup = setup;
        }

        public string GetHandMesh(bool left)
        {
            string mesh = left ? "character_hand_l" : "character_hand_r";
            if (_setup.Weapon == HumanWeapon.Blade)
                mesh += "_0";
            else if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
                mesh += "_ah_0";
            else if (_setup.Weapon == HumanWeapon.Thunderspear)
                mesh += "_ts";
            return mesh;
        }

        public string GetArmMesh(bool left)
        {
            string mesh = "player";
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
            {
                return mesh + (left ? "_casual_arm_AH_L" : "_casual_arm_AH_R");
            }
            else if (_setup.CurrentCostume["Type"].Value.StartsWith("Uniform"))
                mesh += "_uniform";
            else
                mesh += "_casual";
            return mesh + (left ? "_arm_L" : "_arm_R");
        }

        public string Get3dmgMesh()
        {
            return (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG) ? "3dmg_2" : "3dmg";
        }

        public string GetBeltMesh()
        {
            return (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG) ? string.Empty : "3dmg_belt";
        }

        public string GetGasMesh(bool left)
        {
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
                return left ? "char_gun_mag_l" : "char_gun_mag_r";
            return left ? "scabbard_L" : "scabbard_R";
        }

        public string GetWeaponMesh(bool left)
        {
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
                return left ? "character_gun_l_0" : "character_gun_r_0";
            else if (_setup.Weapon == HumanWeapon.Thunderspear)
                return left ? "thunderspear_l" : "thunderspear_r";
            return left ? "blade_L" : "blade_R";
        }

        public string GetBodyMesh()
        {
            string mesh = "player";
            string type = _setup.CurrentCostume["Type"].Value;
            mesh += type.StartsWith("Uniform") ? "_uniform" : "_casual";
            mesh += _setup.CustomSet.Sex.Value == (int)HumanSex.Male ? "_M" : "_F";
            mesh += type.EndsWith("A") ? "A" : "B";
            return mesh;
        }

        public string GetBrandMesh(int brand)
        {
            string type = _setup.CurrentCostume["Type"].Value;
            if (type.StartsWith("Uniform"))
            {
                if (brand == 1)
                    return "character_brand_arm_l_0";
                else if (brand == 2)
                    return "character_brand_arm_r_0";
                else if (brand == 3)
                    return _setup.CustomSet.Sex.Value == (int)HumanSex.Male ? "character_brand_chest_m_0" : "character_brand_chest_f_0";
                else if (brand == 4)
                    return _setup.CustomSet.Sex.Value == (int)HumanSex.Male ? "character_brand_back_m_0" : "character_brand_back_f_0";
            }
            return string.Empty;
        }

        public string GetEyeMesh()
        {
            return "char_eyes";
        }

        public string GetFaceMesh()
        {
            return "char_face";
        }

        public string GetGlassMesh()
        {
            return "char_glasses";
        }

        public string GetHairMesh()
        {
            return _setup.CurrentHair["Texture"].Value;
        }

        public string GetHairClothMesh()
        {
            if (_setup.CurrentHair.HasKey("Cloth"))
                return _setup.CurrentHair["Cloth"].Value;
            return string.Empty;
        }

        public string GetCapeMesh()
        {
            if (_setup.CustomSet.Cape.Value == 0)
                return string.Empty;
            return "character_cape_0";
        }

        public string GetChestMesh(int chest)
        {
            if (chest == 1)
            {
                if (_setup.CurrentCostume.HasKey("Hoodie"))
                {
                    if (_setup.CurrentCostume["Type"].Value.StartsWith("Uniform"))
                        return "char_cap_uni";
                    return "char_cap_cas";
                }
            }
            else if (chest == 2)
            {
                if (_setup.CurrentCostume.HasKey("Holster"))
                {
                    if (_setup.CustomSet.Sex.Value == (int)HumanSex.Male)
                        return "body_blade_keeper_m_0";
                    return "body_blade_keeper_f_0";
                }
            }
            else if (chest == 3)
            {
                if (_setup.CurrentCostume.HasKey("Scarf"))
                {
                    if (_setup.CurrentCostume["Type"].Value.StartsWith("Uniform"))
                        return "mikasa_asset_uni_0";
                    return "mikasa_asset_cas_0";
                }
            }
            return string.Empty;
        }
    }
}
