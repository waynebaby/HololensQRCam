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
using WebCamPhotoService;

public class QRPhotoTakerXAML : Singleton<QRPhotoTakerXAML>
{



    private void Start()
    {
#if WINDOWS_UWP
        if (!Capture.IsInitialized)
        {

            Capture.InitializeAsync();
        }

#endif
        loopCoroutine = StartCoroutine(PhotoTakingLoop());
    }
    Coroutine loopCoroutine;
    public static IPhotoService Capture;
    MemoryStream newFrame;
    MemoryStream oldFrame;
    bool CamInited
    {
        get
        {
            return Capture.IsInitialized && Capture.IsPreviewing;
        }
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
            Capture.GetPhotoStreamAsync().ContinueWith(t =>
            {
                try
                {
                    output = t.Result;
                }
                catch (Exception ex)
                {

                    output = new MemoryStream();
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
                QRCodeReader.Instance.ScanCode(imageData, Capture.PhotoWidth, Capture.PhotoHeight);
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
        if (Capture != null)
        {

            Capture.Dispose();
        }


        base.OnDestroy();
    }

    public bool IsTakingPhotoEnabled;
    public float SecondsBetweenTakes;
}
