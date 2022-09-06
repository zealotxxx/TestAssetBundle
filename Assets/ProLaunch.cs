using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProLaunch : MonoBehaviour
{

    /// <summary>
    /// 显示下载状态和进度
    /// </summary>
    public Text UpdateText;
    public Text DownText;

    public Button btnCheckAndUpdate;
    public Button btnUpdate;
    public Button btnDown;
    public Button btnLogin;
    public Slider Slider;//滑动条组件

    private List<object> _updateKeys = new List<object>();

    // Start is called before the first frame update
    void Start()
    {
        //retryBtn.gameObject.SetActive(false);
        btnCheckAndUpdate.onClick.AddListener(() =>
        {
            StartCoroutine(DoUpdateAddressadble());
        });
        btnUpdate.onClick.AddListener(() =>
        {
            UpdateCatalog();
        });
        // 默认自动执行一次更新检测
        //StartCoroutine(DoUpdateAddressadble());

        btnDown.onClick.AddListener(() =>
        {
            DownLoad();
        });

        btnLogin.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(1);

            //StartCoroutine(LoadScene("Test2"));

        });
    }
    // Update is called once per frame
    void Update()
    {
    }
    public async void UpdateCatalog()
    {
        //初始化Addressable
        var init = Addressables.InitializeAsync();
        await init.Task;

        //开始连接服务器检查更新
        var handle = Addressables.CheckForCatalogUpdates(false);
        await handle.Task;
        Debug.Log("check catalog status " + handle.Status);
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            List<string> catalogs = handle.Result;
            if (catalogs != null && catalogs.Count > 0)
            {
                foreach (var catalog in catalogs)
                {
                    Debug.Log("catalog  " + catalog);
                }
                Debug.Log("download catalog start ");

                UpdateText.text = UpdateText.text + "\n下载更新catalog";
                var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                await updateHandle.Task;
                foreach (var item in updateHandle.Result)
                {
                    Debug.Log("catalog result " + item.LocatorId);

                    foreach (var key in item.Keys)
                    {
                        Debug.Log("catalog key " + key);
                    }
                    _updateKeys.AddRange(item.Keys);
                }
                Debug.Log("download catalog finish " + updateHandle.Status);

                UpdateText.text = UpdateText.text + "\n更新catalog完成" + updateHandle.Status;
            }
            else
            {
                Debug.Log("dont need update catalogs");
                UpdateText.text = "没有需要更新的catalogs信息";
            }
        }
        Addressables.Release(handle);

    }


    public void DownLoad()
    {
        StartCoroutine(DownAssetImpl());
    }

    public IEnumerator DownAssetImpl()
    {
        var downloadsize = Addressables.GetDownloadSizeAsync(_updateKeys);
        yield return downloadsize;
        Debug.Log("start download size :" + downloadsize.Result);
        UpdateText.text = UpdateText.text + "\n更新文件大小" + downloadsize.Result;

        if (downloadsize.Result > 0)
        {
            var download = Addressables.DownloadDependenciesAsync(_updateKeys, Addressables.MergeMode.Union);
            yield return download;
            //await download.Task;
            Debug.Log("download result type " + download.Result.GetType());
            UpdateText.text = UpdateText.text + "\n下载结果类型 " + download.Result.GetType();


            foreach (var item in download.Result as List<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>)
            {
                var ab = item.GetAssetBundle();
                Debug.Log("ab name " + ab.name);

                UpdateText.text = UpdateText.text + "\n ab名称 " + ab.name;


                foreach (var name in ab.GetAllAssetNames())
                {
                    Debug.Log("asset name " + name);
                    UpdateText.text = UpdateText.text + "\n asset 名称 " + name;

                }
            }
            Addressables.Release(download);
        }
        Addressables.Release(downloadsize);
    }


    IEnumerator LoadScene(string senceName)
    {
        // 异步加载场景(如果场景资源没有下载，会自动下载)，

        var handle = Addressables.LoadSceneAsync(senceName);
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError("场景加载异常: " + handle.OperationException.ToString());
            yield break;
        }
        while (!handle.IsDone)
        {
            // 进度（0~1）
            float percentage = handle.PercentComplete;
            Debug.Log("进度: " + percentage);
            yield return null;
        }

        Debug.Log("场景加载完毕");
    }


    IEnumerator DoUpdateAddressadble()
    {
        AsyncOperationHandle<IResourceLocator> initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        // 检测更新
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;
        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            OnError("CheckForCatalogUpdates Error\n" + checkHandle.OperationException.ToString());
            yield break;
        }

        if (checkHandle.Result.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            yield return updateHandle;

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                OnError("UpdateCatalogs Error\n" + updateHandle.OperationException.ToString());
                yield break;
            }

            // 更新列表迭代器
            List<IResourceLocator> locators = updateHandle.Result;
            foreach (var locator in locators)
            {
                List<object> keys = new List<object>();
                keys.AddRange(locator.Keys);
                // 获取待下载的文件总大小
                var sizeHandle = Addressables.GetDownloadSizeAsync(keys);
                yield return sizeHandle;
                if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    OnError("GetDownloadSizeAsync Error\n" + sizeHandle.OperationException.ToString());
                    yield break;
                }

                long totalDownloadSize = sizeHandle.Result;
                UpdateText.text = UpdateText.text + "\ndownload size : " + totalDownloadSize;
                Debug.Log("download size : " + totalDownloadSize);
                if (totalDownloadSize > 0)
                {
                    // 下载
                    var downloadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union, false);
                    //yield return downloadHandle;
                    while (!downloadHandle.IsDone)
                    {
                        if (downloadHandle.Status == AsyncOperationStatus.Failed)
                        {
                            OnError("DownloadDependenciesAsync Error\n" + downloadHandle.OperationException.ToString());
                            yield break;
                        }
                        // 下载进度
                        float percentage = downloadHandle.PercentComplete;


                        Debug.Log($"已下载: {percentage}");
                        DownText.text = $"已下载: {Mathf.Round(percentage * 100)}%";
                        Slider.value = percentage;
                        if (percentage >= 0.9f)//如果进度条已经到达90%
                        {
                            Slider.value = 1; //那就让进度条的值编变成1
                        }

                        yield return null;
                    }
                    yield return downloadHandle;
                    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log("下载完毕!");
                        DownText.text = DownText.text + " 下载完毕";
                    }
                }
            }
        }
        else
        {
            UpdateText.text = UpdateText.text + "\n没有检测到更新";
        }

        // 进入游戏
        EnterPro();
    }
    // 进入游戏
    void EnterPro()
    {
        // TODO
        UpdateText.text = UpdateText.text + "\n进入游戏场景";
        Debug.Log("进入游戏");
    }
    private void OnError(string msg)
    {
        UpdateText.text = UpdateText.text + $"\n{msg}\n请重试! ";
    }



}
