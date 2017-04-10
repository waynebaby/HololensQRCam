using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class QRResultManager : Singleton<QRResultManager>
{



    void OnScanUrlReuslt(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            //if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                ButtonOK.GetComponent<Button>().interactable = true;
                UrlText.GetComponent<Text>().text = url;
                gameObject.GetComponent<AudioSource>().Play();
                MessageText.GetComponent<Text>().text = "Air Tap Okay Button to play";
                LastUrl = url;
            }
        }

    }
    public GameObject UrlText;
    public GameObject MessageText;
    public GameObject ButtonOK;
    public GameObject ButtonCancel;
    string LastUrl;

    public void ExecuteButtonOK()
    {
#if UNITY_WSA_10_0

#endif
        StartCoroutine(LoadLevel());
    }
    IEnumerator LoadLevel()
    {
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Main");
     
    }
    public void ExecuteButtonCancel()
    {
        StartCoroutine(LoadLevel());


    }

}
