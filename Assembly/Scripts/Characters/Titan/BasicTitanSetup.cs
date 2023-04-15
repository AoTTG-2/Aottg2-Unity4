using System;
using UnityEngine;
using ApplicationManagers;
using GameManagers;
using UnityEngine.UI;
using Utility;
using Controllers;
using CustomSkins;
using System.Collections.Generic;
using SimpleJSONFixed;
using System.Collections;
using System.IO;

namespace Characters
{
    class BasicTitanSetup: MonoBehaviour
    {
        public static JSONNode Info;

        public static void Init()
        {
            // Info = JSON.Parse(AssetBundleManager.TryLoadText("TitanSetupInfo"));
            Info = JSON.Parse(File.ReadAllText(FolderPaths.TesterData + "/TitanSetupInfo.json"));
        }

        public static int[] GetRandomBodyHeadCombo()
        {
            int[] result = new int[2];
            var combos = Info["BodyHeadCombos"];
            List<object> nodes = new List<object>();
            List<float> weights = new List<float>();
            foreach (JSONNode node in combos)
            {
                nodes.Add(node);
                weights.Add(node["Chance"].AsFloat);
            }
            var combo = (JSONNode)Util.GetRandomFromWeightedList(nodes, weights);
            result[0] = combo["Body"].AsInt;
            result[1] = combo["Head"].AsInt;
            return result;
        }

        public string CreateRandomSetupJson(int headPrefab)
        {
            var json = new JSONObject();
            json.Add("HeadPrefab", headPrefab);
            json.Add("HairPrefab", Info["HairPrefabs"].GetRandomItem());
            json.Add("HairColor", Info["HairColors"].GetRandomItem());
            json.Add("EyeTexture", UnityEngine.Random.Range(0, Info["EyeTextureCount"].AsInt));
            return json.ToString();
        }

        public void Load(string jsonString)
        {
            var json = JSON.Parse(jsonString);
            var head = transform.Find("Amarture_VER2/Core/Controller.Body/hip/spine/chest/neck/head");
            var headIndex = json["HeadPrefab"].AsInt;
            float gray = UnityEngine.Random.Range(0.7f, 1f);
            var bodyColor = new Color(gray, gray, gray);
            transform.Find("Body").GetComponent<SkinnedMeshRenderer>().material.color = bodyColor;

            // hair
            var hairSocket = head.Find("HairSocket");
            var hair = AssetBundleManager.InstantiateAsset<GameObject>(json["HairPrefab"].Value, true);
            hair.transform.SetParent(hairSocket);
            hair.transform.localPosition = Vector3.zero;
            hair.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            hair.transform.localScale = Vector3.one;
            foreach (Renderer renderer in hair.GetComponentsInChildren<Renderer>())
                renderer.material.color = json["HairColor"].ToColor();

            // head
            string headAsset = "TitanHead" + headIndex.ToString();
            var headMesh = transform.Find("Head");
            var headRef = ((GameObject)AssetBundleManager.LoadAsset(headAsset, true)).transform;
            headMesh.GetComponent<SkinnedMeshRenderer>().material = transform.Find("Body").GetComponent<SkinnedMeshRenderer>().material;
            headMesh.GetComponent<SkinnedMeshRenderer>().sharedMesh = headRef.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            string headColliderAsset = "TitanHeadCollider" + headIndex.ToString();
            headRef = ((GameObject)AssetBundleManager.LoadAsset(headColliderAsset, true)).transform;
            CopyColliders(headRef, head, true, false);
            CopyColliders(headRef.Find("Bone"), head.Find("Bone"), false, false);
            CopyColliders(headRef.Find("EyesHurtbox"), head.Find("EyesHurtbox"), false, true);
            var hairRef = headRef.Find("HairSocket");
            var hairTo = head.Find("HairSocket");
            hairTo.localPosition = hairRef.localPosition;
            hairTo.localRotation = hairRef.localRotation;
            hairTo.localScale = hairRef.localScale;
            if (headRef.Find("Nose") != null)
            {
                var noseRef = headRef.Find("Nose");
                var nose = new GameObject();
                nose.layer = PhysicsLayer.TitanPushbox;
                nose.transform.SetParent(head);
                nose.transform.localPosition = noseRef.localPosition;
                nose.transform.localRotation = noseRef.localRotation;
                nose.transform.localScale = noseRef.localScale;
                var collider = nose.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = Vector3.one;
            }

            // eyes
            string eyesAsset = "TitanEyes" + headIndex.ToString();
            int eyeTexture = json["EyeTexture"].AsInt;
            if (eyeTexture == 7)
                eyeTexture = 0;
            var eyes = transform.Find("Eyes");
            var eyesRef = ((GameObject)AssetBundleManager.LoadAsset(eyesAsset, true)).transform;
            eyes.GetComponent<SkinnedMeshRenderer>().sharedMesh = eyesRef.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            int col = eyeTexture / 8;
            int row = eyeTexture % 8;
            eyes.GetComponent<SkinnedMeshRenderer>().material.mainTextureOffset = new Vector2(0.25f * col, -0.125f * row);
        }

        protected void CopyColliders(Transform from, Transform to, bool capsule, bool moveTransform)
        {
            if (capsule)
            {
                var fromCollider = from.GetComponent<CapsuleCollider>();
                var toCollider = to.GetComponent<CapsuleCollider>();
                toCollider.center = fromCollider.center;
                toCollider.radius = fromCollider.radius;
                toCollider.height = fromCollider.height;
                if (moveTransform)
                {
                    toCollider.transform.localPosition = fromCollider.transform.localPosition;
                    toCollider.transform.localRotation = fromCollider.transform.localRotation;
                    toCollider.transform.localScale = fromCollider.transform.localScale;
                }
            }
            else
            {
                var fromCollider = from.GetComponent<BoxCollider>();
                var toCollider = to.GetComponent<BoxCollider>();
                toCollider.center = fromCollider.center;
                toCollider.size = fromCollider.size;
                if (moveTransform)
                {
                    toCollider.transform.localPosition = fromCollider.transform.localPosition;
                    toCollider.transform.localRotation = fromCollider.transform.localRotation;
                    toCollider.transform.localScale = fromCollider.transform.localScale;
                }
            }
        }
    }
}
