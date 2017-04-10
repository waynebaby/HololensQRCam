using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZXing;
using System.Linq;
public class QRCodeReader : Singleton<QRCodeReader>
{



    private BarcodeReader codeReader;
    string currentResultText="";
    String lastetResultText="";

    // Use this for initialization
    void Start()
    {
        codeReader = new BarcodeReader
        {
            AutoRotate = true,
            TryInverted = true
        };
        codeReader.Options.TryHarder = true;
        StartCoroutine(ReaderChangeLoop());
    }


    IEnumerator ReaderChangeLoop()
    {
        while (true)
        {
            yield return new WaitWhile(() => currentResultText == lastetResultText);

            lastetResultText = currentResultText;
            SendMessage("OnScanUrlReuslt",currentResultText);

        }
    }

    public void ScanCode(Color32[] imageData, int width = 0, int height = 0)
    {
        string resultText=null;
        if (width == 0 || height == 0)
        {
            width = Screen.width;
            height = Screen.height;
        }

        if (codeReader != null && imageData != null && imageData.Length > 256)
        {
            try
            {
                //resultText = "try decode" + imageData.Length;
                var result = codeReader.Decode(imageData, width, height);
                if (result != null)
                {
                    resultText = result.Text;

                }
                else
                {
                    resultText = "";
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                //resultText = ex.Message;
            }

            currentResultText = resultText;
        }
    }

}
