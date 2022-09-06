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
    /// ��ʾ����״̬�ͽ���
    /// </summary>
    public Text UpdateText;
    public Text DownText;

    public Button btnCheckAndUpdate;
    public Button btnUpdate;
    public Button btnDown;
    public Button btnLogin;
    public Slider Slider;//���������

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
        // Ĭ���Զ�ִ��һ�θ��¼��
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
        //��ʼ��Addressable
        var init = Addressables.InitializeAsync();
        await init.Task;

        //��ʼ���ӷ�����������
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

                UpdateText.text = UpdateText.text + "\n���ظ���catalog";
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

                UpdateText.text = UpdateText.text + "\n����catalog���" + updateHandle.Status;
            }
            else
            {
                Debug.Log("dont need update catalogs");
                UpdateText.text = "û����Ҫ���µ�catalogs��Ϣ";
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
        UpdateText.text = UpdateText.text + "\n�����ļ���С" + downloadsize.Result;

        if (downloadsize.Result > 0)
        {
            var download = Addressables.DownloadDependenciesAsync(_updateKeys, Addressables.MergeMode.Union);
            yield return download;
            //await download.Task;
            Debug.Log("download result type " + download.Result.GetType());
            UpdateText.text = UpdateText.text + "\n���ؽ������ " + download.Result.GetType();


            foreach (var item in download.Result as List<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>)
            {
                var ab = item.GetAssetBundle();
                Debug.Log("ab name " + ab.name);

                UpdateText.text = UpdateText.text + "\n ab���� " + ab.name;


                foreach (var name in ab.GetAllAssetNames())
                {
                    Debug.Log("asset name " + name);
                    UpdateText.text = UpdateText.text + "\n asset ���� " + name;

                }
            }
            Addressables.Release(download);
        }
        Addressables.Release(downloadsize);
    }


    IEnumerator LoadScene(string senceName)
    {
        // �첽���س���(���������Դû�����أ����Զ�����)��

        var handle = Addressables.LoadSceneAsync(senceName);
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError("���������쳣: " + handle.OperationException.ToString());
            yield break;
        }
        while (!handle.IsDone)
        {
            // ���ȣ�0~1��
            float percentage = handle.PercentComplete;
            Debug.Log("����: " + percentage);
            yield return null;
        }

        Debug.Log("�����������");
    }


    IEnumerator DoUpdateAddressadble()
    {
        AsyncOperationHandle<IResourceLocator> initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        // ������
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

            // �����б������
            List<IResourceLocator> locators = updateHandle.Result;
            foreach (var locator in locators)
            {
                List<object> keys = new List<object>();
                keys.AddRange(locator.Keys);
                // ��ȡ�����ص��ļ��ܴ�С
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
                    // ����
                    var downloadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union, false);
                    //yield return downloadHandle;
                    while (!downloadHandle.IsDone)
                    {
                        if (downloadHandle.Status == AsyncOperationStatus.Failed)
                        {
                            OnError("DownloadDependenciesAsync Error\n" + downloadHandle.OperationException.ToString());
                            yield break;
                        }
                        // ���ؽ���
                        float percentage = downloadHandle.PercentComplete;


                        Debug.Log($"������: {percentage}");
                        DownText.text = $"������: {Mathf.Round(percentage * 100)}%";
                        Slider.value = percentage;
                        if (percentage >= 0.9f)//����������Ѿ�����90%
                        {
                            Slider.value = 1; //�Ǿ��ý�������ֵ����1
                        }

                        yield return null;
                    }
                    yield return downloadHandle;
                    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log("�������!");
                        DownText.text = DownText.text + " �������";
                    }
                }
            }
        }
        else
        {
            UpdateText.text = UpdateText.text + "\nû�м�⵽����";
        }

        // ������Ϸ
        EnterPro();
    }
    // ������Ϸ
    void EnterPro()
    {
        // TODO
        UpdateText.text = UpdateText.text + "\n������Ϸ����";
        Debug.Log("������Ϸ");
    }
    private void OnError(string msg)
    {
        UpdateText.text = UpdateText.text + $"\n{msg}\n������! ";
    }



}
