using Attribute;
using UnityEngine;

namespace ResourceManager
{
	public class ResourceLoadInfo : MonoBehaviour
	{
		[LabelText("Prefab Object"), ReadOnly]
		public Object prefabObject;

        [TextArea, ReadOnly]
		public string prefabPath;

		[LabelText("Root Object"), ReadOnly]
		public Object rootObject;
	}
}
