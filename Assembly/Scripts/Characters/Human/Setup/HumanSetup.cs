using Xft;
using UnityEngine;
using SimpleJSONFixed;
using ApplicationManagers;
using Utility;
using Settings;
using CustomSkins;
using GameManagers;

namespace Characters
{
    class HumanSetup: MonoBehaviour
    {
        public static JSONNode CostumeInfo;
        public static JSONNode HairInfo;
        public static Material WeaponTrailMaterial;
        public GameObject ThunderspearL;
        public GameObject ThunderspearR;
        public GameObject ThunderspearLModel;
        public GameObject ThunderspearRModel;
        public GameObject _mount_chest;
        public GameObject _mount_3dmg;
        public GameObject _mount_gas_l;
        public GameObject _mount_gas_r;
        public GameObject _mount_gun_mag_l;
        public GameObject _mount_gun_mag_r;
        public GameObject _mount_weapon_l;
        public GameObject _mount_weapon_r;
        public GameObject _part_3dmg;
        public GameObject _part_belt;
        public GameObject _part_gas_l;
        public GameObject _part_gas_r;
        public GameObject _part_arm_l;
        public GameObject _part_arm_r;
        public GameObject _part_asset_1;
        public GameObject _part_asset_2;
        public GameObject _part_blade_l;
        public GameObject _part_blade_r;
        public GameObject _part_brand_1;
        public GameObject _part_brand_2;
        public GameObject _part_brand_3;
        public GameObject _part_brand_4;
        public GameObject _part_cape;
        public GameObject _part_chest;
        public GameObject _part_chest_1;
        public GameObject _part_chest_2;
        public GameObject _part_chest_3;
        public GameObject _part_eye;
        public GameObject _part_face;
        public GameObject _part_glass;
        public GameObject _part_hair;
        public GameObject _part_hair_1;
        public GameObject _part_hair_2;
        public GameObject _part_hand_l;
        public GameObject _part_hand_r;
        public GameObject _part_head;
        public GameObject _part_leg;
        public GameObject _part_upper_body;
        public GameObject _mount_cloth;
        public HumanSetupMeshes _meshes;
        public HumanSetupTextures _textures;

        // loaded settings from player spawning
        public HumanCustomSet CustomSet;
        public HumanWeapon Weapon;
        public JSONNode CurrentCostume;
        public JSONNode CurrentHair;
        public XWeaponTrail LeftTrail1;
        public XWeaponTrail RightTrail1;
        public XWeaponTrail LeftTrail2;
        public XWeaponTrail RightTrail2;
        public bool IsDeadBody;
        public bool Deleted = false;

        public static void Init()
        {
            JSONNode costume = JSON.Parse(AssetBundleManager.TryLoadText("CostumeInfo"));
            CostumeInfo = costume["Costume"];
            HairInfo = costume["Hair"];
            HumanSetupMaterials.Init();
            LoadWeaponTrail();
        }

        private static void LoadWeaponTrail()
        {
            var go = ResourceManager.InstantiateAsset<GameObject>("Character/character_blade_l");
            WeaponTrailMaterial = go.transform.Find("X-WeaponTrailA").GetComponent<XWeaponTrail>().MyMaterial;
            Destroy(go);
        }

        private void Awake()
        {
            _meshes = new HumanSetupMeshes(this);
            _textures = new HumanSetupTextures(this);
            _part_head = transform.Find("character_head_f").gameObject;
            _part_leg = transform.Find("character_leg").gameObject;
            _part_chest = transform.Find("character_chest").gameObject;
            _part_head.transform.parent = transform.Find("Amarture/Controller_Body/hip/spine/chest/neck/head").transform;
            _mount_chest = CreateMount("spine/chest");
            _mount_3dmg = CreateMount("spine/chest");
            _mount_gas_l = CreateMount("spine");
            _mount_gas_r = CreateMount("spine");
            _mount_gun_mag_l = CreateMount("thigh_L");
            _mount_gun_mag_r = CreateMount("thigh_R");
            _mount_weapon_l = CreateMount("spine/chest/shoulder_L/upper_arm_L/forearm_L/hand_L");
            _mount_weapon_r = CreateMount("spine/chest/shoulder_R/upper_arm_R/forearm_R/hand_R");
            _mount_cloth = _part_leg;
        }

        public static int GetCostumeCount(bool male)
        {
            if (male)
                return CostumeInfo["Male"].AsArray.Count;
            return CostumeInfo["Female"].AsArray.Count;
        }

        public static int GetHairCount()
        {
            return HairInfo.AsArray.Count;
        }

        public void Copy(InGameCharacterSettings settings)
        {
            var customSets = SettingsManager.HumanCustomSettings.CustomSets;
            int setIndex = settings.CustomSet.Value;
            int costumeIndex = settings.Costume.Value;
            int preCount = SettingsManager.HumanCustomSettings.Costume1Sets.Sets.GetCount();
            if (setIndex < preCount)
            {
                if (costumeIndex == 1)
                    CustomSet = (HumanCustomSet)SettingsManager.HumanCustomSettings.Costume2Sets.Sets.GetItemAt(setIndex);
                else if (costumeIndex == 2)
                    CustomSet = (HumanCustomSet)SettingsManager.HumanCustomSettings.Costume3Sets.Sets.GetItemAt(setIndex);
                else
                    CustomSet = (HumanCustomSet)SettingsManager.HumanCustomSettings.Costume1Sets.Sets.GetItemAt(setIndex);
            }
            else
                CustomSet = (HumanCustomSet)customSets.Sets.GetItemAt(setIndex - preCount);
            var loadout = settings.Loadout.Value;
            if (loadout == HumanLoadout.Blades)
                Weapon = HumanWeapon.Blade;
            else if (loadout == HumanLoadout.Guns)
                Weapon = HumanWeapon.Gun;
            else if (loadout == HumanLoadout.Thunderspears)
                Weapon = HumanWeapon.Thunderspear;
        }

        public void Load(HumanCustomSet customSet, HumanWeapon weapon, bool isDeadBody = false)
        {
            CustomSet = customSet;
            Weapon = weapon;
            IsDeadBody = isDeadBody;
            if (CustomSet.Sex.Value == (int)HumanSex.Male)
            {
                CurrentCostume = CostumeInfo["Male"][CustomSet.Costume.Value];
                CurrentHair = HairInfo["Male"][CustomSet.Hair.Value];
            }
            else
            {
                CurrentCostume = CostumeInfo["Female"][CustomSet.Costume.Value];
                CurrentHair = HairInfo["Female"][CustomSet.Hair.Value];
            }
            string hair = CustomSet.Hair.Value;
            if (hair.StartsWith("HairM"))
                CurrentHair = HairInfo["Male"][int.Parse(hair.Substring(5))];
            else if (hair.StartsWith("HairF"))
                CurrentHair = HairInfo["Female"][int.Parse(hair.Substring(5))];
            if (CurrentCostume == null)
                DebugConsole.Log("Warning: costume does not exist in CostumeInfo JSON.");
            if (CurrentHair == null)
                DebugConsole.Log("Warning: hair does not exist in CostumeInfo JSON");
            DeleteParts();
            CreateParts();
        }

        public void DeleteDie()
        {
            if (Deleted)
                return;
            DeleteParts();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
            if (LeftTrail1 != null)
            {
                LeftTrail1.Deactivate();
                LeftTrail2.Deactivate();
                RightTrail1.Deactivate();
                RightTrail2.Deactivate();
            }    
            Deleted = true;
        }

        public void CreateParts()
        {
            CreateHead();
            CreateUpperBody();
            CreateArms();
            CreateLowerBody();
            Create3dmg();
            CreateWeapon();
        }

        public void DeleteParts()
        {
            if (!IsDeadBody)
            {
                ClothFactory.DisposeObject(_part_hair_1);
                ClothFactory.DisposeObject(_part_hair_2);
                ClothFactory.DisposeObject(_part_cape);
                ClothFactory.DisposeObject(_part_chest_3);
            }
            DestroyIfExists(_part_eye);
            DestroyIfExists(_part_face);
            DestroyIfExists(_part_glass);
            DestroyIfExists(_part_hair);
            DestroyIfExists(_part_upper_body);
            DestroyIfExists(_part_arm_l);
            DestroyIfExists(_part_arm_r);
            DestroyIfExists(_part_brand_1);
            DestroyIfExists(_part_brand_2);
            DestroyIfExists(_part_brand_3);
            DestroyIfExists(_part_brand_4);
            DestroyIfExists(_part_chest_1);
            DestroyIfExists(_part_chest_2);
            DestroyIfExists(_part_3dmg);
            DestroyIfExists(_part_belt);
            DestroyIfExists(_part_gas_l);
            DestroyIfExists(_part_gas_r);
            DestroyIfExists(_part_blade_l);
            DestroyIfExists(_part_blade_r);
            DestroyIfExists(ThunderspearL);
            DestroyIfExists(ThunderspearR);
        }

        public void Create3dmg()
        {
            DestroyIfExists(_part_3dmg);
            DestroyIfExists(_part_belt);
            DestroyIfExists(_part_gas_l);
            DestroyIfExists(_part_gas_r);
            Material material = HumanSetupMaterials.Materials[_textures.Get3dmgTexture()];
            _part_3dmg = ResourceManager.InstantiateAsset<GameObject>("Character/" + _meshes.Get3dmgMesh(), cached: true);
            AttachToMount(_part_3dmg, _mount_3dmg);
            _part_3dmg.renderer.material = material;
            string beltMesh = _meshes.GetBeltMesh();
            if (beltMesh != string.Empty)
            {
                _part_belt = this.GenerateCloth("Character/" + beltMesh);
                _part_belt.renderer.material = material;
            }
            _part_gas_l = ResourceManager.InstantiateAsset<GameObject>("Character/" + _meshes.GetGasMesh(left: true), cached: true);
            _part_gas_l.renderer.material = material;
            if (Weapon == HumanWeapon.Gun)
                AttachToMount(_part_gas_l, _mount_gun_mag_l);
            else
                AttachToMount(_part_gas_l, _mount_gas_l);
            _part_gas_r = ResourceManager.InstantiateAsset<GameObject>("Character/" + _meshes.GetGasMesh(left: false), cached: true);
            _part_gas_r.renderer.material = material;
            if (Weapon == HumanWeapon.Gun)
                AttachToMount(_part_gas_r, _mount_gun_mag_r);
            else
                AttachToMount(_part_gas_r, _mount_gas_r);
        }

        public void CreateWeapon()
        {
            DestroyIfExists(_part_blade_l);
            DestroyIfExists(_part_blade_r);
            DestroyIfExists(ThunderspearL);
            DestroyIfExists(ThunderspearR);
            if (Weapon == HumanWeapon.Gun || Weapon == HumanWeapon.Blade)
            {
                Material material = HumanSetupMaterials.Materials[_textures.Get3dmgTexture()];
                string weaponLMesh = _meshes.GetWeaponMesh(left: true);
                if (weaponLMesh != string.Empty)
                {
                    _part_blade_l = ResourceManager.InstantiateAsset<GameObject>("Character/" + weaponLMesh, cached: true);
                    AttachToMount(_part_blade_l, _mount_weapon_l);
                    _part_blade_l.renderer.material = material;
                    if (_part_blade_l.transform.Find("X-WeaponTrailA") != null)
                    {
                        LeftTrail1 = _part_blade_l.transform.Find("X-WeaponTrailA").GetComponent<XWeaponTrail>();
                        LeftTrail2 = _part_blade_l.transform.Find("X-WeaponTrailB").GetComponent<XWeaponTrail>();
                        LeftTrail1.Deactivate();
                        LeftTrail2.Deactivate();
                    }
                }
                string weaponRMesh = _meshes.GetWeaponMesh(left: false);
                if (weaponRMesh != string.Empty)
                {
                    _part_blade_r = ResourceManager.InstantiateAsset<GameObject>("Character/" + weaponRMesh, cached: true);
                    AttachToMount(_part_blade_r, _mount_weapon_r);
                    _part_blade_r.renderer.material = material;
                    if (_part_blade_r.transform.Find("X-WeaponTrailA") != null)
                    {
                        RightTrail1 = _part_blade_l.transform.Find("X-WeaponTrailA").GetComponent<XWeaponTrail>();
                        RightTrail2 = _part_blade_l.transform.Find("X-WeaponTrailB").GetComponent<XWeaponTrail>();
                        RightTrail1.Deactivate();
                        RightTrail2.Deactivate();
                    }
                }
            }
            else if (Weapon == HumanWeapon.Thunderspear)
            {
                ThunderspearL = AssetBundleManager.InstantiateAsset<GameObject>("ThunderspearProp");
                ThunderspearR = AssetBundleManager.InstantiateAsset<GameObject>("ThunderspearProp");
                ThunderspearLModel = ThunderspearL.transform.Find("ThunderspearModel").gameObject;
                ThunderspearRModel = ThunderspearR.transform.Find("ThunderspearModel").gameObject;
                AttachThunderspear(ThunderspearL, _mount_weapon_l.transform.parent, true);
                AttachThunderspear(ThunderspearR, _mount_weapon_r.transform.parent, false);
            }
        }

        private void AttachThunderspear(GameObject thunderSpear, Transform mount, bool left)
        {
            thunderSpear.transform.parent = mount.parent;
            Vector3 localPosition = left ? new Vector3(-0.001649f, 0.000775f, -0.000227f) : new Vector3(-0.001649f, -0.000775f, -0.000227f);
            Quaternion localRotation = left ? Quaternion.Euler(5f, -85f, 10f) : Quaternion.Euler(-5f, -85f, -10f);
            thunderSpear.transform.localPosition = localPosition;
            thunderSpear.transform.localRotation = localRotation;
        }

        public void CreateCape()
        {
            if (!IsDeadBody)
            {
                ClothFactory.DisposeObject(_part_cape);
                string capeMesh = _meshes.GetCapeMesh();
                if (capeMesh != string.Empty)
                {
                    _part_cape = ClothFactory.GetCape(_mount_cloth, "Character/" + capeMesh, HumanSetupMaterials.Materials[_textures.GetBrandTexture()]);
                }
            }
        }

        public void CreateHair()
        {
            DestroyIfExists(_part_hair);
            if (!IsDeadBody)
                ClothFactory.DisposeObject(_part_hair_1);
            string hairMesh = _meshes.GetHairMesh();
            if (hairMesh != string.Empty)
            {
                _part_hair = ResourceManager.InstantiateAsset<GameObject>("Character/" + hairMesh, cached: true);
                AttachToMount(_part_hair, _part_head);
                _part_hair.renderer.material = HumanSetupMaterials.Materials[_textures.GetHairTexture()];
                _part_hair.renderer.material.color = CustomSet.HairColor.Value.ToColor();
            }
            string hairClothMesh = _meshes.GetHairClothMesh();
            if (hairClothMesh != string.Empty && !IsDeadBody)
            {
                Material material = HumanSetupMaterials.Materials[_textures.GetHairTexture()];
                _part_hair_1 = ClothFactory.GetHair(_mount_cloth, "Character/" + hairClothMesh, material, CustomSet.HairColor.Value.ToColor());
            }
        }

        public void CreateEye()
        {
            DestroyIfExists(_part_eye);
            _part_eye = ResourceManager.InstantiateAsset<GameObject>("Character/" + _meshes.GetEyeMesh(), cached: true);
            AttachToMount(_part_eye, _part_head);
            SetFacialTexture(_part_eye, CustomSet.Eye.Value);
        }

        public void CreateFace()
        {
            DestroyIfExists(_part_face);
            _part_face = ResourceManager.InstantiateAsset<GameObject>("Character/" + _meshes.GetFaceMesh(), cached: true);
            AttachToMount(_part_face, _part_head);
            string face = CustomSet.Face.Value.Substring(4);
            if (face != "None")
                SetFacialTexture(_part_face, int.Parse(face) + 32);
            else
                SetFacialTexture(_part_face, -1);
        }

        public void CreateGlass()
        {
            _part_glass = ResourceManager.InstantiateAsset<GameObject>("Character/" + _meshes.GetGlassMesh(), cached: true);
            AttachToMount(_part_glass, _part_head);
            string glass = CustomSet.Glass.Value.Substring(5);
            if (glass != "None")
                SetFacialTexture(_part_glass, int.Parse(glass) + 48);
            else
                SetFacialTexture(_part_glass, -1);
        }

        public void CreateHead()
        {
            CreateHair();
            CreateEye();
            CreateFace();
            CreateGlass();
        }

        public void CreateArms()
        {
            DestroyIfExists(_part_arm_l);
            DestroyIfExists(_part_hand_l);
            DestroyIfExists(_part_arm_r);
            DestroyIfExists(_part_hand_r);
            Material bodyMaterial = HumanSetupMaterials.Materials[_textures.GetBodyTexture()];
            Material skinMaterial = HumanSetupMaterials.Materials[_textures.GetSkinTexture()];
            _part_arm_l = GenerateCloth("Character/" + _meshes.GetArmMesh(left: true));
            _part_arm_l.renderer.material = bodyMaterial;
            _part_hand_l = GenerateCloth("Character/" + _meshes.GetHandMesh(left: true));
            _part_hand_l.renderer.material = skinMaterial;
            _part_arm_r = GenerateCloth("Character/" + _meshes.GetArmMesh(left: false));
            _part_arm_r.renderer.material = bodyMaterial;
            _part_hand_r = GenerateCloth("Character/" + _meshes.GetHandMesh(left: false));
            _part_hand_r.renderer.material = skinMaterial;
        }

        public void CreateLowerBody()
        {
            _part_leg.renderer.material = HumanSetupMaterials.Materials[_textures.GetBodyTexture()];
        }

        public void CreateUpperBody()
        {
            DestroyIfExists(_part_upper_body);
            DestroyIfExists(_part_brand_1);
            DestroyIfExists(_part_brand_2);
            DestroyIfExists(_part_brand_3);
            DestroyIfExists(_part_brand_4);
            DestroyIfExists(_part_chest_1);
            DestroyIfExists(_part_chest_2);
            if (!IsDeadBody)
            {
                ClothFactory.DisposeObject(_part_chest_3);
            }
            CreateCape();
            string chest1Mesh = _meshes.GetChestMesh(1);
            if (chest1Mesh != string.Empty)
            {
                _part_chest_1 = ResourceManager.InstantiateAsset<GameObject>("Character/" + chest1Mesh, cached: true);
                AttachToMount(_part_chest_1, _mount_chest);
                _part_chest_1.renderer.material = HumanSetupMaterials.Materials[_textures.GetChestTexture(1)];
            }
            Material bodyMaterial = HumanSetupMaterials.Materials[_textures.GetBodyTexture()];
            string chest2Mesh = _meshes.GetChestMesh(2);
            if (chest2Mesh != string.Empty)
            {
                _part_chest_2 = ResourceManager.InstantiateAsset<GameObject>("Character/" + chest2Mesh, cached: true);
                AttachToMount(_part_chest_2, _mount_chest);
                _part_chest_2.renderer.material = bodyMaterial;
            }
            string chest3Mesh = _meshes.GetChestMesh(3);
            if (chest3Mesh != string.Empty && !IsDeadBody)
            {
                _part_chest_3 = ClothFactory.GetCape(_mount_cloth, "Character/" + chest3Mesh, bodyMaterial);
            }
            _part_upper_body = GenerateCloth("Character/" + _meshes.GetBodyMesh());
            _part_upper_body.renderer.material = bodyMaterial;
            Material brandMaterial = HumanSetupMaterials.Materials[_textures.GetBrandTexture()];
            if (CurrentCostume["Type"].Value.StartsWith("Uniform"))
            {
                _part_brand_1 = GenerateCloth("Character/" + _meshes.GetBrandMesh(1));
                _part_brand_1.renderer.material = brandMaterial;
                _part_brand_2 = GenerateCloth("Character/" + _meshes.GetBrandMesh(2));
                _part_brand_2.renderer.material = brandMaterial;
                _part_brand_3 = GenerateCloth("Character/" + _meshes.GetBrandMesh(3));
                _part_brand_3.renderer.material = brandMaterial;
                _part_brand_4 = GenerateCloth("Character/" + _meshes.GetBrandMesh(4));
                _part_brand_4.renderer.material = brandMaterial;
            }
            Material skinMaterial = HumanSetupMaterials.Materials[_textures.GetSkinTexture()];
            _part_head.renderer.material = skinMaterial;
            _part_chest.renderer.material = skinMaterial;
        }

        private void SetFacialTexture(GameObject go, int id)
        {
            if (id >= 0)
            {
                go.renderer.material = HumanSetupMaterials.Materials[_textures.GetFaceTexture()];
                float num = 0.125f;
                float x = num * ((int)(((float)id) / 8f));
                float y = -num * (id % 8);
                go.renderer.material.mainTextureOffset = new Vector2(x, y);
            }
            else
                go.renderer.material = MaterialCache.TransparentMaterial;
        }

        public void SetSkin()
        {
            Material material = HumanSetupMaterials.Materials[_textures.GetSkinTexture()];
            _part_head.renderer.material = material;
            _part_chest.renderer.material = material;
            _part_hand_l.renderer.material = material;
            _part_hand_r.renderer.material = material;
        }

        private GameObject CreateMount(string transformPath)
        {
            GameObject mount = new GameObject();
            transformPath = "Amarture/Controller_Body/hip/" + transformPath;
            Transform baseTransform = transform;
            mount.transform.position = baseTransform.position;
            mount.transform.rotation = Quaternion.Euler(270f, baseTransform.rotation.eulerAngles.y, 0f);
            mount.transform.parent = baseTransform.Find(transformPath);
            return mount;
        }

        private GameObject GenerateCloth(string cloth)
        {
            SkinnedMeshRenderer meshRenderer = _mount_cloth.GetComponent<SkinnedMeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = _mount_cloth.AddComponent<SkinnedMeshRenderer>();
            Transform[] bones = meshRenderer.bones;
            SkinnedMeshRenderer newMeshRenderer = ResourceManager.InstantiateAsset<GameObject>(cloth, cached: true).GetComponent<SkinnedMeshRenderer>();
            newMeshRenderer.gameObject.transform.parent = meshRenderer.gameObject.transform.parent;
            newMeshRenderer.transform.localPosition = Vector3.zero;
            newMeshRenderer.transform.localScale = Vector3.one;
            newMeshRenderer.bones = bones;
            newMeshRenderer.quality = SkinQuality.Bone4;
            return newMeshRenderer.gameObject;
        }

        private void AttachToMount(GameObject obj, GameObject mount)
        {
            obj.transform.position = mount.transform.position;
            obj.transform.rotation = mount.transform.rotation;
            obj.transform.parent = mount.transform.parent;
        }

        private void DestroyIfExists(GameObject go)
        {
            if (go != null)
                Destroy(go);
        }
    }
}
