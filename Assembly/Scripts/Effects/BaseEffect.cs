using Settings;
using UnityEngine;

namespace Effects
{
    class BaseEffect: MonoBehaviour
    {
        protected PhotonPlayer _owner;
        protected float _timeLeft;

        public virtual void Setup(PhotonPlayer owner, float liveTime, object[] settings)
        {
            _owner = owner;
            _timeLeft = liveTime;
        }

        protected virtual void Update()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
                Destroy(gameObject);
        }
    }
}
