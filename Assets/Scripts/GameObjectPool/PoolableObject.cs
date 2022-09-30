using System;
using Attribute;
using Common;
using UnityEngine;
using UnityEngine.Events;

namespace GameObjectPool
{
	public static class PoolableObjectExtensions
	{
		public static int GetPoolObjectShowCount(this GameObject gameObject)
		{
			if (gameObject.TryGetComponent(out PoolableObject poolableObject))
			{
				return poolableObject.ShowCount;
			}

			return -1;
		}

		public static void SetPoolableObjectHold(this GameObject gameObject, bool hold)
		{
			if (gameObject.TryGetComponent(out PoolableObject poolableObject))
			{
				poolableObject.hold = hold;
			}
		}
		
		public static void SetActiveFalseWithPool(this GameObject go, bool force = false)
		{
			if (go.IsNull())
			{
				return;
			}

			if (force || false == go.activeInHierarchy)
			{
				if (go.TryGetComponent(out PoolableObject poolableObject))
				{
					poolableObject.ToDisable();
				}
			}

			go.SetActive(false);
		}
		
		public static void AddOnActiveAction(this GameObject go, UnityAction<int> action)
		{
			go.DoAncestorOneAction<PoolableObject>((poolableObject) =>
			{
				poolableObject.AddOnActive(action);
			});
		}
		
		public static void AddOnDeActiveAction(this GameObject go, UnityAction<int> action)
		{
			go.DoAncestorOneAction<PoolableObject>((poolableObject) =>
			{
				poolableObject.AddOnDeActive(action);
			});
		}
	}

	public class PoolableObject : MonoBehaviour, IDestroyable, IInstantiatable
	{
		[HideInInspector]
		public string sPrefabPath = "";

		[HideInInspector]
		public GameObject goAsset;

		public Vector3 originalLocalPosition;
		public Quaternion originalLocalRotation;
		public Vector3 originalLocalScale;

		public bool holdParent;
		public bool hold;
		public bool manualReturnToPool;
		public bool resetTransformReturnToPool;
		
		[HideInInspector]
		public bool isLoaded;

		[HideInInspector]
		public bool isDestroyed;

		private int _showCount;
		public int ShowCount => _showCount;
		
		private readonly UnityEvent<int> _onActive = new();
		private readonly UnityEvent<int> _onDeActive = new();

		#if UNITY_EDITOR
		[ReadOnly]
		public bool isInUse;
		#endif

		public void OnInstantiate()
		{
			var tr = transform;
			originalLocalPosition = tr.localPosition;
			originalLocalRotation = tr.localRotation;
			originalLocalScale = tr.localScale;
		}

		public void AddShowCount()
		{
			_showCount++;
		}

		public void OnActive()
		{
			_onActive?.Invoke(_showCount);
		}

		public void OnDeActive()
		{
			_onDeActive?.Invoke(_showCount);
		}

		public void AddOnActive(UnityAction<int> action)
		{
			_onActive?.RemoveListener(action);
			_onActive?.AddListener(action);
		}
		
		public void AddOnDeActive(UnityAction<int> action)
		{
			_onDeActive?.RemoveListener(action);
			_onDeActive?.AddListener(action);
		}

		void OnEnable()
		{
			if (false == isLoaded)
			{
				return;
			}
			
			ToEnable();
		}

		void OnDisable()
		{
			if (false == manualReturnToPool)
			{
				ToDisable();	
			}
		}

		void ToEnable()
		{
			GameObjectPoolManager.Instance.MoveToEnabledObjectPool(goAsset, this);
		}

		public void ToDisable()
		{
			if (resetTransformReturnToPool)
			{
				var tr = transform;
				tr.localPosition = originalLocalPosition;
				tr.localRotation = originalLocalRotation;
				tr.localScale = originalLocalScale;	
			}

			GameObjectPoolManager.Instance.MoveToDisabledObjectPool(goAsset, this);
		}

		public void OnDestroyNow()
		{
			isDestroyed = true;
		}

		void OnDestroy()
		{
			GameObjectPoolManager.Instance.OnDestroyObject(this);
		}
	}
}
