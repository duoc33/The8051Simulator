using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class VMProgramInit
{
    public static VMProgramInit Instance
    {
        get 
        {
            if(instance==null) instance = new VMProgramInit();
            return instance;
        }
    }
    private static VMProgramInit instance;
    private VMProgramInit() 
    {
        isRunning = false;
        VM_Thread = null;
    }
    public bool isRunning;
    public UnityAction CloseAction;//进程关闭后，清零数据，复原场景
    public UnityAction StartAction;
    private Thread VM_Thread;
    public void load_progrom(string path)
    {
        if (VM_Thread != null && VM_Thread.IsAlive)
        {
            close_progrom();
        }
        isRunning = true;
        StartAction?.Invoke();
        VM_Thread = new Thread(StartVMRuntime);
        VM_Thread.Start(path);
    }
    public void close_progrom()
    {
        if (VM_Thread != null && VM_Thread.IsAlive)
        {
            VM_8051_Mono.Instance.isRunning = false;
            VM_8051_Mono.Instance.Reset();
            VM_Thread.Abort();
            VM_Thread = null;
        }
        isRunning = false;
        CloseAction?.Invoke();
    }
    private void StartVMRuntime(object path)
    {
        string path_str = path.ToString();
        if (!File.Exists(path_str))
        {
            return;
        }
        byte[] code = FileTool.ExtractHexData(path_str);
        if (code == null)
        {
            Debug.Log("hexfile loads failed");
            return;
        }
        VM_8051_Mono.Instance.isRunning = true;
        VM_8051_Mono.Instance.Reset();
        VM_8051_Mono.Instance.LoadProgram(code);
        VM_8051_Mono.Instance.Run();
    }
}
