using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.UI;

// �����²�������Դ
public class AddressableUpdater : MonoBehaviour
{
    /// <summary>
    /// ��ʾ����״̬�ͽ���
    /// </summary>
    public Text updateText;

    /// <summary>
    /// ���԰�ť
    /// </summary>
    public Button retryBtn;

    void Start()
    {
        retryBtn.gameObject.SetActive(false);
        retryBtn.onClick.AddListener(() =>
        {
            StartCoroutine(DoUpdateAddressadble());
        });

        // Ĭ���Զ�ִ��һ�θ��¼��
        StartCoroutine(DoUpdateAddressadble());
    }

    IEnumerator DoUpdateAddressadble()
    {
        AsyncOperationHandle<IResourceLocator> initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        // ������
        var checkHandle = Addressables.CheckForCatalogUpdates();
        yield return checkHandle;
        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            OnError("CheckForCatalogUpdates Error\n" + checkHandle.OperationException.ToString());
            yield break;
        }

        if (checkHandle.Result.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, true);
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
                var sizeHandle = Addressables.GetDownloadSizeAsync(keys.GetEnumerator());
                yield return sizeHandle;
                if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    OnError("GetDownloadSizeAsync Error\n" + sizeHandle.OperationException.ToString());
                    yield break;
                }

                long totalDownloadSize = sizeHandle.Result;
                updateText.text = updateText.text + "\ndownload size : " + totalDownloadSize;
                Debug.Log("download size : " + totalDownloadSize);
                if (totalDownloadSize > 0)
                {
                    // ����
                    var downloadHandle = Addressables.DownloadDependenciesAsync(keys, true);
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
                        updateText.text = updateText.text + $"\n������: {percentage}";
                        yield return null;
                    }
                    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log("�������!");
                        updateText.text = updateText.text + "\n�������";
                    }
                }
            }
        }
        else
        {
            updateText.text = updateText.text + "\nû�м�⵽����";
        }

        // ������Ϸ
        EnterGame();
    }

    // �쳣��ʾ
    private void OnError(string msg)
    {
        updateText.text = updateText.text + $"\n{msg}\n������! ";
        // ��ʾ���԰�ť
        retryBtn.gameObject.SetActive(true);
    }


    // ������Ϸ
    void EnterGame()
    {
        // TODO
    }
}