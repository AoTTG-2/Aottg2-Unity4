using UnityEngine;
using System.Collections.Generic;
using Utility;
using GameManagers;
using System.Collections;
using ApplicationManagers;

namespace Characters
{
    class BaseHitbox: MonoBehaviour
    {
        public BaseCharacter Owner;
        public bool OnEnter = true;
        protected HashSet<GameObject> _hitGameObjects = new HashSet<GameObject>();
        public Collider _collider;
        protected bool _firstHit = false;
        public GameObject _debugObject;

        public static BaseHitbox Create(BaseCharacter owner, GameObject obj, Collider collider = null)
        {
            BaseHitbox hitbox = obj.AddComponent<BaseHitbox>();
            hitbox.Owner = owner;
            if (collider == null)
                collider = obj.GetComponent<Collider>();
            hitbox._collider = collider;
            hitbox.Deactivate();
            if (DebugTesting.DebugColliders)
            {
                if (collider is SphereCollider)
                {
                    var sphere = AssetBundleManager.InstantiateAsset<GameObject>("TestSphere");
                    sphere.transform.parent = obj.transform;
                    sphere.transform.localPosition = ((SphereCollider)collider).center;
                    sphere.transform.localScale = Vector3.one * ((SphereCollider)collider).radius * 2f;
                    sphere.GetComponent<Renderer>().material.color = Color.red;
                    hitbox._debugObject = sphere;
                    hitbox._debugObject.SetActive(false);
                }
                else if (collider is CapsuleCollider)
                {
                    var capsule = AssetBundleManager.InstantiateAsset<GameObject>("TestCapsule");
                    capsule.transform.parent = obj.transform;
                    capsule.transform.localPosition = ((CapsuleCollider)collider).center;
                    capsule.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    float radius = ((CapsuleCollider)collider).radius;
                    float height = ((CapsuleCollider)collider).height;
                    capsule.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
                    capsule.GetComponent<Renderer>().material.color = Color.red;
                    hitbox._debugObject = capsule;
                    hitbox._debugObject.SetActive(false);
                }
            }
            return hitbox;
        }

        public bool IsActive()
        {
            return _collider.enabled;
        }
        
        public void Activate(float delay = 0f, float length = 0f)
        {
            _hitGameObjects.Clear();
            _firstHit = true;
            if (delay == 0f)
            {
                _collider.enabled = true;
                ToggleDebug(true);
            }
            else
                StartCoroutine(WaitAndActivate(delay));
            if (length > 0f)
                StartCoroutine(WaitAndDeactivate(delay + length));
        }

        public void Deactivate()
        {
            StopAllCoroutines();
            _collider.enabled = false;
            ToggleDebug(false);
        }

        protected IEnumerator WaitAndActivate(float delay)
        {
            yield return new WaitForSeconds(delay);
            _collider.enabled = true;
            ToggleDebug(true);
        }

        protected IEnumerator WaitAndDeactivate(float delay)
        {
            yield return new WaitForSeconds(delay);
            _collider.enabled = false;
            ToggleDebug(false);
            
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!OnEnter)
                return;
            OnTrigger(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (OnEnter)
                return;
            OnTrigger(other);
        }

        protected virtual void OnTrigger(Collider other)
        {
            var go = other.transform.root.gameObject;
            BaseCharacter character = go.GetComponent<BaseCharacter>();
            if (character != null && !TeamInfo.SameTeam(Owner, character) && !_hitGameObjects.Contains(other.gameObject))
            {
                _hitGameObjects.Add(other.gameObject);
                OnHit(character, other);
            }
        }

        protected virtual void OnHit(BaseCharacter victim, Collider collider)
        {
            Owner.OnHit(this, victim, collider, "", _firstHit);
            _firstHit = false;
        }

        protected void ToggleDebug(bool toggle)
        {
            if (_debugObject != null)
                _debugObject.SetActive(toggle);
        }
    }
}
