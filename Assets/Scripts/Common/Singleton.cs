using UnityEngine;

namespace Common
{
	public abstract class Singleton<T> where T : class, new()
	{
		private static T _instance;

		public static bool IsNull()
		{
			return _instance.IsNull(false);
		}

		public static T Instance
		{
			get
			{
				if (null == _instance)
				{
#if UNITY_EDITOR
					string typeName = typeof(T).ToString();
					Debug.Log($"Singleton {typeName} Create Start");
#endif
					
					_instance = new T();
					
#if UNITY_EDITOR
					Debug.Log($"Singleton {typeName} Create Finish");
#endif
				}

				return _instance;
			}
		}
	}
}
