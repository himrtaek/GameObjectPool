using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ResourceManager
{
	public sealed class ResourceManager : Singleton<ResourceManager>
	{
		public ResourceManager()
		{
#if UNITY_EDITOR
			BindOnPostLoad(onPostResourceLoad);
#endif
		}
		
		public void onPostResourceLoad(UnityEngine.Object objAsset, UnityEngine.Object objInstance)
		{
			var goAsset = objAsset as GameObject;
			var goInstance = objInstance as GameObject;
			if (null != goAsset && null != goInstance)
			{
				goInstance.DoChildAllAction((goChild) =>
				{
					if (false == goChild.TryGetComponent(out ResourceLoadInfo comp))
					{
						comp = goChild.AddComponent<ResourceLoadInfo>();
					}

					if (comp.rootObject == null)
					{
						comp.prefabObject = objAsset;
#if UNITY_EDITOR
						comp.prefabPath = null == objAsset ? "" : UnityEditor.AssetDatabase.GetAssetPath(objAsset);
#endif
						comp.rootObject = objInstance;
					}
				});
			}
		}
		
		public static string GetResourceFileName(Object assetObject)
		{
#if UNITY_EDITOR
			const string resourcesDir = "resources/";

			if (assetObject != null)
			{
				var path = AssetDatabase.GetAssetPath(assetObject).Replace('\\', '/');
				var pathLower = path.ToLower();
				var resIndex = pathLower.LastIndexOf(resourcesDir, StringComparison.Ordinal);

				if (resIndex >= 0)
				{
					path = path.Substring(resIndex + resourcesDir.Length);
					var dotIndex = path.LastIndexOf(".", StringComparison.Ordinal);
					if (dotIndex >= 0)
					{
						path = path.Substring(0, dotIndex);
					}

					return path;
				}
			        
			}
#endif
			return "";
		}
		
		public void Clear()
		{
			ClearOriginalAssetList();
			ClearPreloadObject();
		}

		#region ## OnPostLoad ##
		public delegate void OnPostLoad(Object objAsset, Object objInstance);
		private OnPostLoad _postLoad;
		
		public void BindOnPostLoad(OnPostLoad onPostLoad)
		{
			if (_postLoad == null)
			{
				_postLoad -= onPostLoad;
			}

			_postLoad += onPostLoad;
		}
		#endregion

		#region ## OriginalAsset ##
		public bool bEnableOriginalAssetPool = true;
		private readonly Dictionary<string, Object> _cachedOriginalAssets = new ();
		
		public void SetOriginalAssetPath(Object obj, string path)
		{
			if (_cachedOriginalAssets.ContainsValue(obj))
			{
#if UNITY_EDITOR
				foreach (var it in _cachedOriginalAssets)
				{
					if (it.Value == obj)
					{
						if (it.Key != path)
						{
							Debug.LogWarning("[Preload] ??????????????? ???????????? ????????? ?????? ????????? ???????????? ????????? ????????????");
						}
						break;
					}
				}
#endif
				return;
			}

			_cachedOriginalAssets[path] = obj;
		}
		
		public string GetOriginalAssetPath(Object obj)
		{
			foreach (var it in _cachedOriginalAssets)
			{
				if (it.Value == obj)
				{
					return it.Key;
				}
			}

			return "";
		}
		
		private T GetOriginalAsset<T>(string assetPath) where T : Object
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return null;
			}

			if (false == _cachedOriginalAssets.TryGetValue(assetPath, out var goAsset))
			{
				// ??????????????? ???????????? ????????? ?????? Object ????????? ???????????? ?????? 1??? ??????????????? ????????????
				return null;
			}

			return goAsset as T;
		}
		
		private async Task<(bool success, T objAsset)> NewOriginalAssetAsync<T>(string assetPath) where T : Object
		{
			var assetAsync = Resources.LoadAsync<T>(assetPath);
			while (!assetAsync.isDone)
			{
				await Task.Yield();
			}

			var asset = assetAsync.asset as T;

			if (asset.IsNull(false))
			{
				Debug.LogError($"NewOriginalAssetAsync : Not found resource path => \"{assetPath}\"");
				return (false, null);
			}

			return (true, asset);
		}
		
		private T NewOriginalAsset<T>(string assetPath, bool showLog = true) where T : Object
		{
			var asset = Resources.Load<T>(assetPath);
			if (null == asset)
			{
				if (showLog)
				{
					Debug.LogError($"NewOriginalAsset : Not found resource path => \"{assetPath}\"");
				}
				return null;
			}

			return asset;
		}
		
		private T GetOrNewOriginalAsset<T>(string assetPath, bool showLog = true) where T : Object
		{
			var objOriginalAsset = GetOriginalAsset<T>(assetPath);
			if (objOriginalAsset)
			{
				return objOriginalAsset;
			}

			objOriginalAsset = NewOriginalAsset<T>(assetPath, showLog);
			if (!objOriginalAsset)
			{
				return null;
			}

			_cachedOriginalAssets[assetPath] = objOriginalAsset;

			return objOriginalAsset;
		}
		
		private async Task<(bool success, T objAsset)> GetOrNewOriginalAssetAsync<T>(string assetPath) where T : Object
		{
			var objOriginalAsset = GetOriginalAsset<T>(assetPath);
			if (null != objOriginalAsset)
			{
				return (true, objOriginalAsset);
			}

			var ret = await NewOriginalAssetAsync<T>(assetPath);
			var asset = ret.objAsset;

			if (asset.IsNull(false))
			{
				return (false, null);
			}

			_cachedOriginalAssets[assetPath] = asset;

			return (true, asset);
		}
		
		public async Task<(bool success, T objAsset)> LoadOriginalAssetAsync<T>(string assetPath, bool useCache = true) where T : Object
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return (false, null);
			}

			return await (useCache ? GetOrNewOriginalAssetAsync<T>(assetPath) : NewOriginalAssetAsync<T>(assetPath));
		}

		public Task<(bool success, T objAsset)> LoadOriginalAssetNoCacheAsync<T>(string assetPath) where T : Object
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return new Task<(bool success, T objAsset)>(() => (false, null));
			}

			return NewOriginalAssetAsync<T>(assetPath);
		}

		public T LoadOriginalAsset<T>(string assetPath, bool useCache = true, bool showLog = true) where T : Object
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return null;
			}

			return useCache ? GetOrNewOriginalAsset<T>(assetPath, showLog) : NewOriginalAsset<T>(assetPath, showLog);
		}

		public T LoadOriginalAssetNoCache<T>(string assetPath, bool showLog = true) where T : Object
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return null;
			}

			return NewOriginalAsset<T>(assetPath, showLog);
		}

		private void ClearOriginalAssetList()
		{
			_cachedOriginalAssets.Clear();
		}
		
		public void UnloadUnusedAssets()
		{
			Resources.UnloadUnusedAssets();
		}
		#endregion

		#region ## ObjectInstance ##
		public T LoadInstance<T>(string sPath, Transform trParent = null, bool bUsePreload = true, bool bShowLog = true) where T : Object
		{
			if (string.IsNullOrEmpty(sPath))
			{
				return null;
			}

			var objOriginalAsset = LoadOriginalAsset<T>(sPath, bUsePreload, false);
			if (objOriginalAsset.IsNull_NoLog())
			{
				if(bShowLog)
					Debug.LogError(
						$"ResourceManager.LoadInstance<{typeof(T).Name}>(\"{sPath}\") : loading failed (not found resource)");
				return null;
			}

			return Instantiate(objOriginalAsset, trParent, bUsePreload);
		}

		public async Task<(bool success, T objAsset)> LoadInstanceAsync<T>(string sPath, Transform trParent = null, bool bUsePreload = true, bool isEditor = false) where T : Object
		{
			if (string.IsNullOrEmpty(sPath))
			{
				return (false, null);
			}

			T asset;
			if (isEditor)
			{
				var objOriginalAsset = LoadOriginalAsset<T>(sPath);
				asset = objOriginalAsset;
			}
			else
			{
				var objOriginalAsset = await LoadOriginalAssetAsync<T>(sPath);
				asset = objOriginalAsset.objAsset;
			}
			
			if (asset.IsNull_NoLog())
			{
				Debug.LogError(
					$"ResourceManager.LoadInstanceAsync<{typeof(T).Name}>(\"{sPath}\") : loading failed (not found resource)");
				return (false, null);
			}

			return (true, Instantiate(asset, trParent, bUsePreload));
		}
		
		public T InstantiateNoPreload<T>(T objSrc, Transform parent=null) where T : Object
		{
			return Instantiate(objSrc, parent, false);
		}
		
		public T Instantiate<T>(T objSrc, bool bUsePreload) where T : Object
		{
			return Instantiate(objSrc, null, bUsePreload);
		}
		
		public T Instantiate<T>(T objSrc, Transform trParent = null, bool bUsePreload = true) where T : Object
		{
			if (objSrc.IsNull_NoLog())
			{
				Debug.LogError("Instantiate : objSrc is null");
				return null;
			}

			var objInstance = bUsePreload ? GetPreloadObject(objSrc, trParent) : null;
			if (objInstance.IsNull_NoLog())
			{
				objInstance = Object.Instantiate(objSrc, trParent);
				if (objInstance.IsNull_NoLog())
				{
					Debug.LogError($"Instantiate : Instance is null (src=\"{objSrc.name}\")");
					return null;
				}

				objInstance.name = objSrc.name;

				_postLoad?.Invoke(objSrc, objInstance);
			}

			return objInstance;
		}
		#endregion

		#region ## Preload (pool) ##
		private Transform _preLoadParent;
		private Transform PreloadParent
		{
			get
			{
				if (false == _preLoadParent.IsNull(false))
				{
					return _preLoadParent;
				}

				var go = new GameObject
				{
					name = "PreloadObject",
					transform =
					{
						localPosition = Vector3.zero,
						localScale = Vector3.one
					}
				};
				go.SetActive(false);

				Object.DontDestroyOnLoad(go);

				_preLoadParent = go.transform;

				return _preLoadParent;
			}
		}

		private readonly Dictionary<Object, List<Object>> _mDictPreloadObject = new ();

		public async Task<bool> AddPreloadObjectAsync<T>(string sPath, int iCount = 1) where T : Object
		{
			if (string.IsNullOrEmpty(sPath))
			{
				return false;
			}

			var ret = await LoadOriginalAssetAsync<T>(sPath);
			if (ret.objAsset.IsNull_NoLog())
			{
				Debug.LogError(
					$"ResourceManager.AddPreloadObjectAsync<{typeof(T).Name}>(\"{sPath}\", {iCount}) : loading failed (resource not found)");
				return false;
			}

			return AddPreloadObject(ret.objAsset, iCount);
		}

		public bool AddPreloadObject<T>(string sPath, int iCount = 1) where T : Object
		{
			if (string.IsNullOrEmpty(sPath))
			{
				return false;
			}

			var objAsset = LoadOriginalAsset<T>(sPath);
			if (false == AddPreloadObject(objAsset, iCount))
			{
				return false;
			}

			return true;
		}

		public bool AddPreloadObject(Object objAsset, int iCount = 1)
		{
			if (objAsset.IsNull())
			{
				return false;
			}

			for (int i = 0; i < iCount; ++i)
			{
				var objInstance = Instantiate(objAsset, PreloadParent, false);
				if (objInstance.IsNull_NoLog())
				{
					Debug.LogError(
						$"ResourceManager.AddPreloadObject(objAsset.name = \"{objAsset.name}\") : instantiate error");
					return false;
				}

				if (objInstance is GameObject go)
				{
					go.SetActive(false);
				}

				if (false == _mDictPreloadObject.TryGetValue(objAsset, out var list))
				{
					list = new List<Object>();
					_mDictPreloadObject[objAsset] = list;
				}

				list.Add(objInstance);
			}

			return true;
		}

		public bool IsPreloadObject<T>(T objAsset) where T : Object
		{
			return _mDictPreloadObject.ContainsKey(objAsset);
		}

		public T GetPreloadObject<T>(T objAsset, Transform trParent = null) where T : Object
		{
			if (objAsset.IsNull())
			{
				return null;
			}

			if (false == _mDictPreloadObject.TryGetValue(objAsset, out var list) || null == list || 0 == list.Count)
			{
				return null;
			}

			var objInstance = list[0];
			list.RemoveAt(0);

			if (objInstance is GameObject go)
			{
				go.transform.SetParent(trParent, false);
				if (trParent.IsNull(false))
				{
					SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());	
				}
				
				go.SetActive(true);
			}

			return objInstance as T;
		}

		private void ClearPreloadObject()
		{
			foreach (var it in _mDictPreloadObject)
			{
				foreach (var it2 in it.Value)
				{
					if (it2.IsNull_NoLog())
					{
						continue;
					}

					if (it2 is GameObject go)
					{
						if (go.IsDestroyed(false))
						{
							continue;
						}

						go.DestroySafe();
					}
					else
					{
						Object.Destroy(it2);
					}
				}

				it.Value.Clear();
			}

			_mDictPreloadObject.Clear();
		}
		#endregion
	}
}
