using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapSegmentPreview : MonoBehaviour
{
    public MeshFilter MeshFilter { get; set; }

	void Start ()
    {
        #if UNITY_EDITOR
        OnStart();
        #else
        GameObject.Destroy(gameObject);
        #endif
    }

    public void OnStart()
    {
        MeshFilter = GetComponent<MeshFilter>();

        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}
