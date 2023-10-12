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
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
                mesh += "_ah";
            return mesh;
        }

        public string GetArmMesh(bool left)
        {
            string mesh = "character_arm";
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
            {
                return mesh + (left ? "_casual_l_ah" : "_casual_r_ah");
            }
            else if (_setup.CurrentCostume["Type"].Value.StartsWith("Uniform"))
                mesh += "_uniform";
            else
                mesh += "_casual";
            return mesh + (left ? "_l" : "_r");
        }

        public string Get3dmgMesh()
        {
            return (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG) ? "character_3dmg_2" : "character_3dmg";
        }

        public string GetBeltMesh()
        {
            return (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG) ? string.Empty : "character_3dmg_belt";
        }

        public string GetGasMesh(bool left)
        {
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
                return left ? "character_gun_mag_l" : "character_gun_mag_r";
            return left ? "character_3dmg_gas_l" : "character_3dmg_gas_r";
        }

        public string GetWeaponMesh(bool left)
        {
            if (_setup.Weapon == HumanWeapon.AHSS || _setup.Weapon == HumanWeapon.APG)
                return left ? "character_gun_l" : "character_gun_r";
            else if (_setup.Weapon == HumanWeapon.Thunderspear)
                return string.Empty;
            return left ? "character_blade_l" : "character_blade_r";
        }

        public string GetBodyMesh()
        {
            string mesh = "character_body";
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
                    return "character_brand_arm_l";
                else if (brand == 2)
                    return "character_brand_arm_r";
                else if (brand == 3)
                    return _setup.CustomSet.Sex.Value == (int)HumanSex.Male ? "character_brand_chest_m" : "character_brand_chest_f";
                else if (brand == 4)
                    return _setup.CustomSet.Sex.Value == (int)HumanSex.Male ? "character_brand_back_m" : "character_brand_back_f";
            }
            return string.Empty;
        }

        public string GetEyeMesh()
        {
            return "character_eye";
        }

        public string GetFaceMesh()
        {
            return "character_face";
        }

        public string GetGlassMesh()
        {
            return "glass";
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
            return "character_cape";
        }

        public string GetChestMesh(int chest)
        {
            if (chest == 1)
            {
                if (_setup.CurrentCostume.HasKey("Hoodie"))
                {
                    if (_setup.CurrentCostume["Type"].Value.StartsWith("Uniform"))
                        return "character_cap_uniform";
                    return "character_cap_casual";
                }
            }
            else if (chest == 2)
            {
                if (_setup.CurrentCostume.HasKey("Holster"))
                {
                    if (_setup.CustomSet.Sex.Value == (int)HumanSex.Male)
                        return "character_body_blade_keeper_m";
                    return "character_body_blade_keeper_f";
                }
            }
            else if (chest == 3)
            {
                if (_setup.CurrentCostume.HasKey("Scarf"))
                {
                    if (_setup.CurrentCostume["Type"].Value.StartsWith("Uniform"))
                        return "mikasa_asset_uni";
                    return "mikasa_asset_cas";
                }
            }
            return string.Empty;
        }
    }
}
