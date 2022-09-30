using System;
using GameObjectPool;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Common
{
	public static class BoolExtensions
	{
		public static bool IsTrue(this bool obj, bool showLog = true)
		{
			if (obj)
			{
				if (showLog)
				{
					Debug.LogError("System.Boolean Is True");
				}

				return true;
			}

			return false;
		}
		
		public static bool IsFalse(this bool obj, bool showLog = true)
		{
			if (false == obj)
			{
				if (showLog)
				{
					Debug.LogError("System.Boolean Is False");
				}

				return true;
			}

			return false;
		}
		
		public static bool Assert(this bool obj, bool showLog = true)
		{
			if (false == obj)
			{
				if (showLog)
				{
					Debug.LogError("System.Boolean Is False");
				}
			}

			return obj;
		}
	}
	
	public static class UnityObjectExtensions
	{
		public static T GetOrAddComponent<T>(this GameObject target) where T : Component
		{
			var result = target.GetComponent<T>();
			if (!result)
			{
				result = target.AddComponent<T>();
			}
			return result;
		}
		
		public static bool SafeIsUnityNull(this Object go)
		{
			return ReferenceEquals(go, null);
		}
		
		public static bool IsNull(this GameObject go, bool showLog = true)
		{
			// FakeNull 이슈
			if (go.SafeIsUnityNull())
			{
				if (showLog)
				{
					Debug.LogError("GameObject Is Null");
				}

				return true;
			}

			return false;
		}
		
		public static bool IsNull(this Object obj, bool showLog = true)
		{
			// FakeNull 이슈
			if (obj.SafeIsUnityNull())
			{
				if (showLog)
				{
					Debug.LogError("Object Is Null");
				}

				return true;
			}

			return false;
		}
		
		public static bool IsNull(this object obj, bool showLog = true)
		{
			if (null == obj)
			{
				if (showLog)
				{
					Debug.LogError("System.Object Is Null");
				}

				return true;
			}

			return false;
		}
		
		public static bool IsNull_NoLog(this Object obj)
		{
			return obj.SafeIsUnityNull();
		}
		
		public static bool IsNull_NoLog(this object obj)
		{
			return obj == null;
		}
	}
	
	public static class GameObjectExtensions
	{
		public static void DoChildAllAction(this GameObject goParent, Action<GameObject> action)
		{
			if (goParent.IsNull())
			{
				return;
			}

			action(goParent);

			for (int i = 0; i < goParent.transform.childCount; ++i)
			{
				var child = goParent.transform.GetChild(i).gameObject;
				if (child.IsDestroyed())
				{
					continue;
				}

				DoChildAllAction(child, action);
			}
		}
		
		public static void DoAncestorOneAction<T>(this GameObject go, Action<T> action) where T : MonoBehaviour
		{
			if (go.IsNull())
			{
				return;
			}

			if (go.TryGetComponent(out T component))
			{
				action(component);
			}
			else
			{
				if (go.IsNull(false))
				{
					return;
				}
				
				DoAncestorOneAction(go.transform.parent.gameObject, action);
			}
		}
		
		public static void DestroySafe(this GameObject go)
		{
			if (go.IsNull(false))
			{
				return;
			}

			go.tag = Tags.Destroyed;

			var components = go.GetComponentsInChildren(typeof(IDestroyable));
			foreach (var comp in components)
			{
				if (comp is not IDestroyable destroyable)
				{
					continue;
				}

				destroyable.OnDestroyNow();
			}

			Object.Destroy(go);
		}
		
		public static bool IsDestroyed(this Object go, bool bShowErrorLog = true)
		{
			return go.IsNull(bShowErrorLog);
		}

		public static bool IsDestroyed(this GameObject go, bool bShowErrorLog = true)
		{
			if (go.IsNull(bShowErrorLog))
			{
				return true;
			}

			return go.CompareTag(Tags.Destroyed);
		}
	}
}
