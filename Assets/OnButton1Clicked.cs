using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public class OnButton1Clicked : MonoBehaviour
{
    public AssetReference reference;
    public Transform parent;
    GameObject obj = null;
    public string AssetToLoad;

    public void ClickEvent()
    {
        Debug.Log("Btn1Clicked");
        if (obj == null)
        {
            Addressables.LoadAssetAsync<GameObject>(AssetToLoad).Completed += (handle) =>
            {
                GameObject prefabObject = handle.Result;
                obj = Instantiate(prefabObject, parent);
            };
        }
        else
        {
            Destroy(obj);
            Addressables.ReleaseInstance(obj);
            obj = null;
        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
