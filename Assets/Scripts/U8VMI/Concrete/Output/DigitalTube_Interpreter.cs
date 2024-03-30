using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DigitalTube_Interpreter : VM_Interpreter
{
    private const byte P0_Addr = Const.P0;
    private const byte P2_Addr = Const.P2;
    private class MaterialInfo 
    {
        public Material[] MatPool;
        public Coroutine[] ColorCoroutines;
    }
    private MaterialInfo matInfo;

    private byte P0_Data = 0;
    private byte P2_234_Data = 0;
    private byte Last_P0_Data = 0;
    private byte Last_P2_234_Data = 0;
    protected override void OnInit()
    {
        Init();//初始化模型材质信息
    }

    #region Core
    
    protected override void OnStateChanged(float elapsedtime) 
    {
        //CancelLast(P2_234_Data);//消除上一次的结果,因为上一次结果保存在P2_234_Data中，再还没有进行数据再次赋值时，将它用作消除上次的结果
        //if (TryGetData(P0_Addr,out byte result0)) 
        //{
        //    P0_Data = result0;
        //}
        //if (TryGetData(P2_234_Data, out byte result1))
        //{
        //    P2_234_Data = (byte)((result1 & 0x1C) >> 2);
        //}
        //Execute(P2_234_Data,P0_Data);//执行当前数据的结果
    }
    private void Init()
    {
        matInfo = new MaterialInfo();
        matInfo.MatPool = new Material[64];
        matInfo.ColorCoroutines = new Coroutine[64];
        for (int i = 0; i < transform.childCount; i++)
        {
            for (int j = 0; j < transform.GetChild(i).childCount; j++)
            {
                matInfo.MatPool[i * 8 + j] = transform.GetChild(i).GetChild(j).GetComponent<MeshRenderer>().material;
            }
        }
    }
    private void Execute(byte bitIndex, byte segIndex)
    {
        for (int i = 0; i < 8; i++)
        {
            int MatStartIndex = bitIndex * 8 + i;
            if (matInfo.ColorCoroutines[MatStartIndex] != null)
            {
                StopCoroutine(matInfo.ColorCoroutines[MatStartIndex]);
                matInfo.ColorCoroutines[MatStartIndex] = null;
            }
            if (((segIndex >> i) & 0x1) == 1)
            {
                matInfo.MatPool[MatStartIndex].SetColor("_Color", Color.red);
            }
            else
            {
                matInfo.MatPool[MatStartIndex].SetColor("_Color", Color.white);
            }
        }
    }
    private void CancelLast(byte lastbitIndex,float switchTime)
    {
        for (int i = 0; i < 8; i++)
        {
            int MatStartIndex = lastbitIndex * 8 + i;
            
            StartColorLerp(MatStartIndex, switchTime, Color.white);
        }
    }
    protected override void OnClose()
    {
        base.OnClose();
        for (int i = 0; i < matInfo.MatPool.Length; i++)
        {
            if (matInfo.ColorCoroutines[i] != null)
            {
                StopCoroutine(matInfo.ColorCoroutines[i]);
                matInfo.ColorCoroutines[i] = null;
            }
            matInfo.MatPool[i].SetColor("_Color", Color.white);
        }
    }

    #endregion

    protected override void OnStart()
    {
        base.OnStart();
        DigitalTubeExecuteLoop();
    }
    private void DigitalTubeExecuteLoop() 
    {
        StartCoroutine(DigitalTubeDetect());
    }

    #region Draw Function Core Field
    public float delayTime = 0;
    public float switchTime = 0.2f;
    public float maxVisiualStay = 0.35f;
    public float minVisiualStay = 0.01f;
    private WaitUntil waitUntil;
    private WaitForEndOfFrame waitForEndOfFrame;
    #endregion
    private IEnumerator DigitalTubeDetect() 
    {
        waitUntil = new WaitUntil(SFR_Detect);
        waitForEndOfFrame = new WaitForEndOfFrame();
        while (IsRunning) 
        {
            delayTime = Time.realtimeSinceStartup;
            yield return waitUntil;
            yield return waitForEndOfFrame;
            delayTime = Time.realtimeSinceStartup - delayTime;
            
            if (delayTime >= maxVisiualStay)
            {
                delayTime = maxVisiualStay - minVisiualStay;
            }
            else if (delayTime <= 0)
            {
                delayTime = 0;
            }
#if true
            switchTime = CalcSwitchTime(delayTime);
            //Debug.Log("delayTime: "+ delayTime+" , switchTime: "+ switchTime);
            delayTime = 0;
            if (P2_234_Data != Last_P2_234_Data) CancelLast(Last_P2_234_Data, switchTime);
            Execute(P2_234_Data, P0_Data);
            Last_P2_234_Data = P2_234_Data;
            Last_P0_Data = P0_Data;
#endif

        }
        yield return null;
    }
    private bool SFR_Detect()
    {
        P2_234_Data = (byte)((Read_SFR(P2_Addr) & 0x1C) >> 2);
        P0_Data = Read_SFR(P0_Addr);
        if (Last_P2_234_Data != P2_234_Data || Last_P0_Data != P0_Data) 
        {
            return true;
        }
        return false;
    }

    private float CalcSwitchTime(float elapseTime)
    {
        float transitionTime = 0;
        //线性时间
#if false
        elapseTime = Mathf.Clamp01(elapseTime / maxVisiualStay - minVisiualStay);
        transitionTime = Mathf.Lerp(maxVisiualStay, minVisiualStay,elapseTime);
#endif

        //Hermite时间
#if true
        transitionTime = HermiteInterpolationTwoThree(elapseTime, 0, maxVisiualStay, maxVisiualStay, minVisiualStay, -5f, 0);
#endif
        return transitionTime;
    }

    private float HermiteInterpolationTwoThree(float x, float x0, float x1, float y0, float y1, float dy0, float dy1)
    {
        float Alpha0 = (1 + 2 * (x - x0) / (x1 - x0)) * Mathf.Pow((x - x1) / (x0 - x1), 2);
        float Alpha1 = (1 + 2 * (x - x1) / (x0 - x1)) * Mathf.Pow((x - x0) / (x1 - x0), 2);
        float Beta0 = (x - x0) * Mathf.Pow((x - x1) / (x0 - x1), 2);
        float Beta1 = (x - x1) * Mathf.Pow((x - x0) / (x1 - x0), 2);
        float H3x = Alpha0 * y0 + Alpha1 * y1 + Beta0 * dy0 + Beta1 * dy1;
        H3x = H3x > 0 ? H3x : 0;
        return H3x;
    }

    private void StartColorLerp(int index, float transitionTime, Color target)
    {
        if (matInfo.ColorCoroutines[index] != null)
        {
            StopCoroutine(matInfo.ColorCoroutines[index]);
            matInfo.ColorCoroutines[index] = null;
        }
        matInfo.ColorCoroutines[index] =
            StartCoroutine(ColorLerp(transitionTime, matInfo.MatPool[index], matInfo.MatPool[index].color, target));
    }
    private IEnumerator ColorLerp(float transitionTime, Material mat, Color current, Color target)
    {
        if (current == target) yield break;
        float elapseTime = 0f;
        while (elapseTime < transitionTime && transitionTime > 0)
        {
            elapseTime += Time.deltaTime;
            Color tempColor = Color.Lerp(current, target, Mathf.Clamp01(elapseTime / transitionTime));
            mat.SetColor("_Color", tempColor);
            yield return null;
        }
        mat.SetColor("_Color", target);
    }

}
