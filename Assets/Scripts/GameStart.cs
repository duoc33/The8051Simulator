using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private const string Led = "/Led.hex";
    private const string DigitalTube = "/DigitalTube.hex";
    private const string DotLed = "/DotLed.hex";
    private const string DotLedShow = "/DotLedShow.hex";
    private const string IndividualKey = "/IndividualKey.hex";
    private const string InteruptControl = "/InteruptControl.hex";
    private const string RectKey = "/RectKey.hex";
    private const string Timer = "/time_Int.hex"; 
    private const string digitalTubeLoop = "/digitalTubeLoop.hex";
    private static string[] paths = new string[9] {
        Application.streamingAssetsPath+ Led, 
            Application.streamingAssetsPath+ DigitalTube,
            Application.streamingAssetsPath+ DotLed,
            Application.streamingAssetsPath+ DotLedShow,
            Application.streamingAssetsPath+ IndividualKey,
            Application.streamingAssetsPath+ RectKey,
            Application.streamingAssetsPath+ InteruptControl,
            Application.streamingAssetsPath+ Timer,
            Application.streamingAssetsPath+ digitalTubeLoop
            };

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0)) { VMProgramInit.Instance.close_progrom();}
        if (Input.GetKeyDown(KeyCode.Alpha1)) { LoadProgram(0);/*Led*/ }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { LoadProgram(1);/*DigitalTube*/ }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { LoadProgram(2); /*DotLed*/}
        if (Input.GetKeyDown(KeyCode.Alpha4)) { LoadProgram(3);/*DotLedShow*/ }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { LoadProgram(4); /*IndividualKey*/}
        if (Input.GetKeyDown(KeyCode.Alpha6)) { LoadProgram(5); /*RectKey*/}
        if (Input.GetKeyDown(KeyCode.Alpha7)) { LoadProgram(6); /*InteruptControl*/}
        if (Input.GetKeyDown(KeyCode.Alpha8)) { LoadProgram(7);/*Timer*/ }
        if (Input.GetKeyDown(KeyCode.Alpha9)) { LoadProgram(8); /*digitalTubeLoop*/}
    }
    private void LoadProgram(int num) 
    {
        VMProgramInit.Instance.close_progrom();
        string path = paths[num];
        if (string.IsNullOrEmpty(path)) return;
        if (!File.Exists(path))
        {
            Debug.LogError("none file");
            return;
        }
        VMProgramInit.Instance.load_progrom(path);
    }
    private void OnDestroy()
    {
        VMProgramInit.Instance.close_progrom();
    }
}
