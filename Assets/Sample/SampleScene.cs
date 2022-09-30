using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Attribute;
using Common;
using GameObjectPool;
using UnityEngine;

namespace Sample
{
    public class SampleScene : MonoBehaviour
    {
        public float speed = 50;
        public float lifeTime = 2;
        
        private int _lastRotation;
        
        [ReadOnly] public long childCount;
        
        private readonly WaitForSeconds _waitForSeconds = new (0.01f);
        private readonly CancellationTokenSource _tokenSource = new();
        
        void Start()
        {
            Preload();

            StartCoroutine(ProcessSpawn());
            ProcessSpawnByTask(_tokenSource.Token);
        }

        private void OnDestroy()
        {
            _tokenSource?.Cancel();
        }

        private static void Preload()
        {
            ResourceManager.ResourceManager.Instance.AddPreloadObject<GameObject>("Cube_1", 100);
            ResourceManager.ResourceManager.Instance.AddPreloadObject<GameObject>("Cube_2", 100);
        }

        private void RefreshChildCount()
        {
            childCount = transform.childCount;
        }
        
        private int GetRotation()
        {
            _lastRotation += 2;
            if (360 <= _lastRotation)
            {
                _lastRotation = 0;
            }

            return _lastRotation;
        }

        async void ProcessSpawnByTask(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                InternalProcessSpawn("Cube_2");

                await Task.Yield();
            }
        }

        IEnumerator ProcessSpawn()
        {
            while (true)
            {
                InternalProcessSpawn("Cube_1");

                yield return _waitForSeconds;
            }
        }

        private void InternalProcessSpawn(string path)
        {
            var go = GameObjectPoolManager.Instance.GetOrNewObject(path, transform, PoolableObjectFlag.ResetTransformReturnToPool);
            if (go.IsNull())
            {
                return;
            }
            
            go.transform.rotation = Quaternion.Euler(new Vector3(0, GetRotation(), 0));

            if (go.TryGetComponent(out MoveForward moveForward).IsFalse())
            {
                return;
            }
            
            moveForward.SetData(speed, lifeTime);

            RefreshChildCount();
        }
    }
}
