using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common;
using ResourceManager;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameObjectPool
{	
	[Flags]
	public enum PoolableObjectFlag
	{
		None = 0,
		
		ParentHold = 1 << 0,
		ManualReturnToPool = 1 << 1,
		ResetTransformReturnToPool = 1 << 2,
	}
	
	public class GameObjectPoolManager : Singleton<GameObjectPoolManager>
	{
		public bool ReuseObject { get; set; } = true;
		private bool IsClearing { get; set; }

		private Transform _poolableParent;
		private Transform PoolableParent
		{
			get
			{
				if (false == _poolableParent.IsNull(false))
				{
					return _poolableParent;
				}

				var go = new GameObject
				{
					name = PoolableParentName,
					transform =
					{
						localPosition = Vector3.zero,
						localScale = Vector3.one
					}
				};
				go.SetActive(false);

				Object.DontDestroyOnLoad(go);

				_poolableParent = go.transform;

				return _poolableParent;
			}
		}
		
		private readonly Dictionary<GameObject, HashSet<PoolableObject>> _dictEnabledObjectPoolByGameObject = new();
		private readonly Dictionary<GameObject, HashSet<PoolableObject>> _dictDisabledObjectPoolByGameObject = new();
		
		private const string PoolableParentName = "PoolableObject";

		public void DumpLog()
		{
			Dictionary<string, int> countByPath = new();
			foreach (var it in _dictEnabledObjectPoolByGameObject)
			{
				if (it.Value.Count <= 0)
				{
					continue;
				}

				if (false == it.Value.First().TryGetComponent(out ResourceLoadInfo resourceLoadInfo).IsFalse())
				{
					if (countByPath.TryGetValue(resourceLoadInfo.prefabPath, out var count))
					{
						countByPath[resourceLoadInfo.prefabPath] = count + it.Value.Count;
					}
					else
					{
						countByPath[resourceLoadInfo.prefabPath] = it.Value.Count;
					}
				}
			}
			
			foreach (var it in _dictDisabledObjectPoolByGameObject)
			{
				if (it.Value.Count <= 0)
				{
					continue;
				}

				if (false == it.Value.First().TryGetComponent(out ResourceLoadInfo resourceLoadInfo).IsFalse())
				{
					if (countByPath.TryGetValue(resourceLoadInfo.prefabPath, out var count))
					{
						countByPath[resourceLoadInfo.prefabPath] = count + it.Value.Count;
					}
					else
					{
						countByPath[resourceLoadInfo.prefabPath] = it.Value.Count;
					}
				}
			}

			foreach (var it in countByPath)
			{
				Debug.Log($"{it.Key} : {it.Value}");
			}
		}
		
		private void AddEnabledObject(GameObject goAsset, PoolableObject poolObject)
		{
			if (goAsset.IsNull())
			{
				return;
			}

			if (poolObject.IsNull())
			{
				return;
			}

			if (false == ReuseObject)
			{
				return;
			}
			
			if (false == _dictEnabledObjectPoolByGameObject.TryGetValue(goAsset, out var list))
			{
				list = new HashSet<PoolableObject>();
				_dictEnabledObjectPoolByGameObject.Add(goAsset, list);
			}

			list.Add(poolObject);
#if UNITY_EDITOR
			poolObject.isInUse = true;
#endif
		}

		private void AddDisabledObject(GameObject goAsset, PoolableObject poolObject)
		{
			if (goAsset.IsNull())
			{
				return;
			}

			if (poolObject.IsNull())
			{
				return;
			}

			// 객체가 삭제중이면 disable object pool에 담지 않는다.
			if (poolObject.gameObject.IsDestroyed())
			{
				return;
			}

			if (false == ReuseObject)
			{
				return;
			}

			if (false == _dictDisabledObjectPoolByGameObject.TryGetValue(goAsset, out var list))
			{
				list = new HashSet<PoolableObject>();
				_dictDisabledObjectPoolByGameObject.Add(goAsset, list);
			}

			list.Add(poolObject);
#if UNITY_EDITOR
			poolObject.isInUse = false;
#endif
			
			poolObject.OnDeActive();
		}

		private PoolableObject GetDisabledObject(GameObject goAsset, Transform trParent)
		{
			if (false == ReuseObject)
			{
				return null;
			}

			if (false == _dictDisabledObjectPoolByGameObject.TryGetValue(goAsset, out var container))
			{
				return null;
			}

			if (container.Count <= 0)
			{
				return null;
			}

			// 부모 변경을 하게되면 느려서, 같은부모먼저 찾음
			foreach (var poolObject in container)
			{
				if (poolObject.transform.parent != trParent)
				{
					continue;
				}

				if (poolObject.hold)
				{
					continue;
				}

				if (poolObject.isDestroyed)
				{
					continue;
				}

				poolObject.gameObject.SetActive(true);
				return poolObject;
			}

			foreach (var poolObject in container)
			{
				if (poolObject.hold)
				{
					continue;
				}

				if (poolObject.isDestroyed)
				{
					continue;
				}

				if (poolObject.holdParent && poolObject.transform.parent != trParent && poolObject.transform.parent != PoolableParent)
				{
					continue;
				}

				poolObject.gameObject.SetActive(true);
				return poolObject;
			}

			return null;
		}

		public void MoveToDisabledObjectPool(GameObject goAsset, PoolableObject poolObject)
		{
			if (IsClearing)
			{
				return;
			}
			
			if (goAsset.IsNull())
			{
				return;
			}

			if (_dictEnabledObjectPoolByGameObject.TryGetValue(goAsset, out var list))
			{
				list.Remove(poolObject);
			}

			if (false == goAsset.IsNull(false))
			{
				AddDisabledObject(goAsset, poolObject);
			}
		}

		public void MoveToEnabledObjectPool(GameObject goAsset, PoolableObject poolObject)
		{
			if (goAsset.IsNull())
			{
				return;
			}
			
			if (_dictDisabledObjectPoolByGameObject.TryGetValue(goAsset, out var list))
			{
				list.Remove(poolObject);
			}

			if (false == goAsset.IsNull(false))
			{
				AddEnabledObject(goAsset, poolObject);
			}
		}

		public GameObject GetOrNewObject(string sPrefabPath, Transform trParent = null, PoolableObjectFlag flag = PoolableObjectFlag.None)
		{
			if (string.IsNullOrEmpty(sPrefabPath))
			{
				return null;
			}

			var goAsset = ResourceManager.ResourceManager.Instance.LoadOriginalAsset<GameObject>(sPrefabPath, true, true);
			return GetOrNewObject(goAsset, trParent, flag);
		}

		public GameObject GetOrNewObject(GameObject goAsset, Transform trParent = null, PoolableObjectFlag flag = PoolableObjectFlag.None)
		{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
			Stopwatch sw = new Stopwatch();
			sw.Start();
#endif
			
			if (goAsset.IsNull())
			{
				return null;
			}
			
			bool bHoldParent = flag.HasFlag(PoolableObjectFlag.ParentHold);
			bool bManualReturnToPool = flag.HasFlag(PoolableObjectFlag.ManualReturnToPool);
			bool bResetTransformReturnToPool = flag.HasFlag(PoolableObjectFlag.ResetTransformReturnToPool);

			var poolObject = GetDisabledObject(goAsset, trParent);
			if (poolObject.IsNull(false))
			{
				var newGameObject = ResourceManager.ResourceManager.Instance.Instantiate(goAsset, trParent);
				if (newGameObject.IsNull())
				{
					return null;
				}

				newGameObject.name = newGameObject.name.Replace("(Clone)", "");

				poolObject = newGameObject.GetOrAddComponent<PoolableObject>();
				if (0 == poolObject.ShowCount)
				{
					poolObject.OnInstantiate();
				}
				poolObject.goAsset = goAsset;
				poolObject.holdParent = bHoldParent;
				poolObject.manualReturnToPool = bManualReturnToPool;
				poolObject.resetTransformReturnToPool = bResetTransformReturnToPool;
				poolObject.isLoaded = true;
				
				AddEnabledObject(goAsset, poolObject);
			}
			else
			{
				MoveToEnabledObjectPool(goAsset, poolObject);
				
				if (poolObject.transform.parent != trParent)
				{
					poolObject.transform.SetParent(trParent, false);
				}
			}

			poolObject.AddShowCount();
			poolObject.OnActive();
			
			return poolObject.gameObject;
		}

		public void OnDestroyObject(PoolableObject poolObject)
		{
			if (poolObject.IsNull())
			{
				return;
			}

			foreach (var it in _dictEnabledObjectPoolByGameObject)
			{
				if (it.Value.Remove(poolObject))
				{
					break;
				}
			}

			foreach (var it in _dictDisabledObjectPoolByGameObject)
			{
				if (it.Value.Remove(poolObject))
				{
					break;
				}
			}
		}

		public void Clear()
		{
			IsClearing = true;

			void ClearContainer(Dictionary<GameObject, HashSet<PoolableObject>> poolContainer)
			{
				foreach (var it in poolContainer)
				{
					foreach (var poolObj in it.Value)
					{
						if (poolObj.gameObject.IsDestroyed())
						{
							continue;
						}
						
						poolObj.gameObject.DestroySafe();
					}
					
					it.Value.Clear();
				}
				poolContainer.Clear();
			}
			
			ClearContainer(_dictEnabledObjectPoolByGameObject);
			ClearContainer(_dictDisabledObjectPoolByGameObject);

			IsClearing = false;
		}

		public int GetPoolableObjectCountByOriginalAsset(GameObject originalAsset)
		{
			var count = 0;
			{
				if (_dictEnabledObjectPoolByGameObject.TryGetValue(originalAsset, out var poolableObjects))
				{
					count += poolableObjects.Count;
				}
			}
			
			{
				if (_dictDisabledObjectPoolByGameObject.TryGetValue(originalAsset, out var poolableObjects))
				{
					count += poolableObjects.Count;
				}
			}

			return count;
		}

		public void AllObjectKeep()
		{
			HashSet<PoolableObject> allObjects = new();
			foreach (var it in _dictEnabledObjectPoolByGameObject)
			{
				foreach (var poolObj in it.Value)
				{
					if (poolObj.gameObject.IsDestroyed())
					{
						continue;
					}

					allObjects.Add(poolObj);
				}
			}

			foreach (var it in allObjects)
			{
				it.ToDisable();
			}
			
			foreach (var it in _dictDisabledObjectPoolByGameObject)
			{
				foreach (var poolObj in it.Value)
				{
					if (poolObj.gameObject.IsDestroyed())
					{
						continue;
					}

					if (poolObj.transform.parent == PoolableParent)
					{
						continue;
					}
					
					poolObj.transform.SetParent(PoolableParent, false);
				}
			}
		}
	}
}
