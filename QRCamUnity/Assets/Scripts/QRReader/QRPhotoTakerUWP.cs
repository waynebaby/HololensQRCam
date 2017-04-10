using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

public class QRPhotoTakerUWP : Singleton<QRPhotoTakerUWP>
{



    private void Start()
    {
#if WINDOWS_UWP
        capture = new WebCamPhotoService.SimplePhotoService();
        StartCoroutine(OpenCamera());
#endif
        loopCoroutine = StartCoroutine(PhotoTakingLoop());
    }
    Coroutine loopCoroutine;
    WebCamPhotoService.SimplePhotoService capture;
    MemoryStream newFrame;
    MemoryStream oldFrame;
    bool CamInited;
    IEnumerator OpenCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        int state = 0;

        capture.InitializeAsync().ContinueWith(t =>
        {
            if (!t.IsFaulted)
            {
                state++;
            }

        }

        );

        yield return new WaitUntil(() => state == 1);

        CamInited = true;
    }


    IEnumerator PhotoTakingLoop()
    {
        yield return new WaitUntil(() => CamInited);
        while (true)
        {
            StartCoroutine(OnTimer());
            yield return new WaitWhile(() => System.Object.ReferenceEquals(oldFrame, newFrame));
            oldFrame = newFrame;
            yield return new WaitForSeconds(SecondsBetweenTakes);
        }
    }





    IEnumerator OnTimer()
    {


        if (IsTakingPhotoEnabled && CamInited)
        {

            MemoryStream output = null;
            capture.GetPhotoStreamAsync().ContinueWith(t =>
            {
                try
                {
                    output = t.Result;
                }
                catch (Exception ex)
                {

                    output = new MemoryStream();
                    Debug.Log(ex.Message);
                }

            });
            yield return new WaitWhile(() => output == null);
            OnCapturedPhotoToMemory(output);

        }




    }

    private void OnCapturedPhotoToMemory(MemoryStream input)
    {



        Task.Factory.StartNew(() =>
        {
            var byteList = input.ToArray();

            Debug.Log("Photo taken");


            //BGRA32
            var targetList = new List<Color32>();

            for (int i = 0; i < byteList.Length; i = i + 4)
            {
                var r = byteList[i + 2];
                var g = byteList[i + 1];
                var b = byteList[i];
                var a = byteList[i + 3];
                targetList.Add(new Color32(r, g, b, a));


            }
            var imageData = targetList.ToArray();

            try
            {
                QRCodeReader.Instance.ScanCode(imageData, capture.PhotoWidth, capture.PhotoHeight);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

                newFrame = input;
            }

        });



    }

    void Update()
    {

    }

    protected override void OnDestroy()
    {

        StopCoroutine(loopCoroutine);
        if (capture != null)
        {

            capture.Dispose();
        }


        base.OnDestroy();
    }

    public bool IsTakingPhotoEnabled;
    public float SecondsBetweenTakes;
}
