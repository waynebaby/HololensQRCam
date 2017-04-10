using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;

public class QRPhotoTaker : Singleton<QRPhotoTaker>
{



    private void Start()
    {
#if !WINDOWS_UWP

        StartCoroutine(OpenCamera());
#endif
        loopCoroutine = StartCoroutine(PhotoTakingLoop());
    }
    Coroutine loopCoroutine;
    PhotoCapture capture;
    PhotoCaptureFrame newFrame;
    PhotoCaptureFrame oldFrame;
    IEnumerator OpenCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        PhotoCapture.CreateAsync(false, pc =>
        {



            capture = pc;



            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            CameraParameters c = new CameraParameters();
            c.hologramOpacity = 0.0f;
            c.cameraResolutionWidth = cameraResolution.width;
            c.cameraResolutionHeight = cameraResolution.height;
            c.pixelFormat = CapturePixelFormat.BGRA32;

            capture.StartPhotoModeAsync(c,
                psr =>
                {
                    if (psr.success)
                    {
                        IsCamInited = true;
                        Debug.Log("Cam Inited");
                    }

                });


        });

    }


    IEnumerator PhotoTakingLoop()
    {
        yield return new WaitUntil(() => IsCamInited);
        while (true)
        {
            OnTimer();
            yield return new WaitWhile(() => System.Object.ReferenceEquals(oldFrame, newFrame));
            oldFrame = newFrame;
            yield return new WaitForSeconds(SecondsBetweenTakes);
        }
    }





    void OnTimer()
    {

        try
        {
            if (IsTakingPhotoEnabled && IsCamInited)
            {


                //if (Application.HasUserAuthorization(UserAuthorization.WebCam))
                //{

                //}

                capture.TakePhotoAsync(OnCapturedPhotoToMemory);
                //var photo = cts.Task.Result;
            }

        }
        catch (System.Exception ex)
        {

            Debug.LogException(ex);

        }
        finally
        {


        }



    }

    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            // Create our Texture2D for use and set the correct resolution
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();


            //var t2d = new Texture2D(cameraResolution.width, cameraResolution.height);
            //photoCaptureFrame.UploadImageDataToTexture(t2d);

            //var imageData = t2d.GetPixels32();



            ////     // Do as we wish with the texture such as apply it to a material, etc.
            Task.Factory.StartNew(() =>
            {
                var byteList = new List<Byte>();

                using (photoCaptureFrame)
                {
                    photoCaptureFrame.CopyRawImageDataIntoBuffer(byteList);
                }


                Debug.Log("Photo taken");


                //BGRA32
                var targetList = new List<Color32>();

                for (int i = 0; i < byteList.Count; i = i + 4)
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
                    QRCodeReader.Instance.ScanCode(imageData, cameraResolution.width, cameraResolution.height);
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {

                    newFrame = photoCaptureFrame;
                }

            });



        }

    }

    void Update()
    {

    }

    protected override void OnDestroy()
    {

        StopCoroutine(loopCoroutine);
        capture.StopPhotoModeAsync(e => { capture.Dispose(); });


        base.OnDestroy();
    }

    public bool IsTakingPhotoEnabled;
    public bool IsCamInited;
    public float SecondsBetweenTakes;
}
