using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

public abstract class VM_Interpreter : MonoBehaviour
{
    #region Base
    private Dictionary<byte, byte> tempDataDic;
    private Dictionary<byte, byte> CheckDic;
    private ConcurrentDictionary<byte, ConcurrentQueue<byte>> DataValues;
    private float elapsedtime;
    private void Start()
    {
        Init();
    }
    private void Init()
    {
        VMProgramInit.Instance.CloseAction += OnClose;
        VMProgramInit.Instance.StartAction += OnStart;
        OnInit();
        if (DataValues == null || DataValues.Count == 0) return;
        VM_8051_Mono.Instance.DataCheckEvent += DataCheck;
    }
    private void Abort()
    {
        OnDestoryed();
        VMProgramInit.Instance.CloseAction -= OnClose;
        VMProgramInit.Instance.StartAction -= OnStart;
        if (DataValues == null || DataValues.Count == 0) return;
        VM_8051_Mono.Instance.DataCheckEvent -= DataCheck;
        CheckDic?.Clear();
        tempDataDic?.Clear();
        DataValues?.Clear();
    }
    private void OnDestroy()
    {
        Abort();
    }
    private void DataCheck(byte[] sfr_ram)
    {
        if (DataValues == null || DataValues.Count == 0) return;
        foreach (byte addr in DataValues.Keys)
        {
            if (!CheckDic.ContainsKey(addr)) CheckDic.Add(addr, sfr_ram[addr]);
            if (CheckDic[addr] != sfr_ram[addr])
            {
                if (DataValues.TryGetValue(addr, out ConcurrentQueue<byte> queue))
                {
                    byte value = sfr_ram[addr];
                    CheckDic[addr] = value;
                    queue.Enqueue(value);
                }
            }
        }
    }
    private void DataOutCheck()
    {
        if (DataValues == null || DataValues.Count == 0) return;
        if (tempDataDic.Count != 0) tempDataDic.Clear();
        foreach (var addr in DataValues.Keys)
        {
            if (DataValues.TryGetValue(addr, out ConcurrentQueue<byte> queue))
            {
                if (queue.Count == 0) continue;
                if (queue.TryDequeue(out byte res))
                {
                    tempDataDic.Add(addr, res);
                }
            }
        }
        if (tempDataDic.Count != 0)
        {
            OnStateChanged(elapsedtime);
            elapsedtime = 0;
        }
    }
    private void Update()
    {
        if (!IsRunning) { elapsedtime = 0; return; }
        elapsedtime += Time.deltaTime;
        DataOutCheck();
        OnUpdate();
    }
    #endregion

    #region Obsolete
    protected void AddInputDataInConcurrnetQueue(byte addr, byte value, bool isBit) => VM_8051_Mono.Instance.AddInputData(addr, value, isBit);

    #endregion

    #region Core
    protected void VMReset() => VM_8051_Mono.Instance.Reset();
    protected bool IsRunning => VMProgramInit.Instance.isRunning;
    protected byte Read_SFR(byte addr)
    {
        if (!IsRunning) return 0;
        return VM_8051_Mono.Instance.Read_Sfr(addr);
    }
    protected void AddInputRegisterCheckAddr(byte addr, SFRInputCheck check) => VM_8051_Mono.Instance.RegisterInputCheckEvent(addr, check);
    protected void AddRegisterCheckAddr(byte addr)
    {
        if (DataValues == null) DataValues = new ConcurrentDictionary<byte, ConcurrentQueue<byte>>();
        if (CheckDic == null) CheckDic = new Dictionary<byte, byte>();
        if (tempDataDic == null) tempDataDic = new Dictionary<byte, byte>();
        if (DataValues.ContainsKey(addr)) return;
        while (DataValues.TryAdd(addr, new ConcurrentQueue<byte>())) { }
        while (CheckDic.TryAdd(addr, 0)) { }
    }

    //获取已经改变的指定寄存器对象数据
    protected bool TryGetData(byte key, out byte result)
    {
        if (tempDataDic == null || tempDataDic.Count == 0) 
        {
            result = 0;
            return false;
        }
        if (tempDataDic.ContainsKey(key))
        {
            result = tempDataDic[key];
            return true;
        }
        result = 0;
        return false;
    }

    //状态改变时，需要执行的逻辑
    protected virtual void OnStateChanged(float elapsedtime) { }
    protected virtual void OnInit() { }
    protected virtual void OnUpdate() { }
    protected virtual void OnDestoryed() { }
    protected virtual void OnClose()
    {
        if (DataValues == null || DataValues.Count == 0) return;
        foreach (ConcurrentQueue<byte> queue in DataValues.Values)
        {
            queue.Clear();
        }
    }
    protected virtual void OnStart() { }
    #endregion
}
