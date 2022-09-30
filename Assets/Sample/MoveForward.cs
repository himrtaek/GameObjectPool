using System;
using GameObjectPool;
using UnityEngine;

namespace Sample
{
    public class MoveForward : MonoBehaviour
    {
        private float _speed = 50;
        private float _lifeTime = 2;

        private float _elapsedTime;

        public void SetData(float speed, float lifeTime)
        {
            _speed = speed;
            _lifeTime = lifeTime;
        }
        
        void Start()
        {
            gameObject.AddOnActiveAction((_) => OnActive());
            gameObject.AddOnDeActiveAction((_) => OnDeActive());
        }

        private void OnActive()
        {
            _elapsedTime = 0;
        }

        private void OnDeActive()
        {
            _elapsedTime = 0;
        }
        
        void Update()
        {
            _elapsedTime += Time.deltaTime;
            if (0 <= _lifeTime && _lifeTime <= _elapsedTime)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.Translate(Vector3.forward * (Time.deltaTime * _speed));
        }
    }
}
