using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.EventSystems;

public class DotMatrix_Interpreter : VM_Interpreter
{
    private const byte P0_Addr = Const.P0;
    private const byte P3_Addr = Const.P3;

    public Transform GndTransform;
    private Light[] lights;
    protected override void OnInit()
    {
        ClickComponent click = GndTransform.gameObject.AddComponent<ClickComponent>();
        click.moveDis = Vector3.right * 0.2f;
        C595.Init();//74HC595芯片功能模拟的结构体数据初始化
        lights = transform.GetComponentsInChildren<Light>();//获取8x8点阵屏灯组件

        AddRegisterCheckAddr(P0_Addr);
        AddRegisterCheckAddr(P3_Addr);
    }

    private byte P0_data;
    private byte C595Register_Data;
    private byte P3_data;
    //串行数据输入 P34
    private bool SER_Data;
    //存储寄存器时钟输入引脚 P35
    private bool RCLK_Data;
    //移位寄存器时钟输入 P36
    private bool SRCLK_Data;
    private bool IsGnd => GndTransform.GetComponent<ClickComponent>().GetState;
    private IO_Extension_74HC595 C595;

    protected override void OnStateChanged(float elapsedtime)
    {
        if (TryGetData(P0_Addr, out byte res0)) 
        {
            P0_data = res0;
        }
        if (TryGetData(P3_Addr, out byte res1)) 
        {
            P3_data = res1;
        }

        if (!IsGnd) 
        {
            return;
        }

        SER_Data = Const.Read_Bit(P3_data,4);
        RCLK_Data = Const.Read_Bit(P3_data, 5);
        SRCLK_Data = Const.Read_Bit(P3_data, 6);
        if (C595.Excute(SER_Data, RCLK_Data, SRCLK_Data)) 
        {
            C595Register_Data = C595.C595RegisterData;
            Excute(P0_data, C595Register_Data);
        }
    }
    private void Excute(byte P0_data,byte C595Data) 
    {
        for (int i = 0; i < 8; i++)
        {
            int row = (byte)(C595Data & (1 << i)); 
            for (int j = 0; j < 8; j++)
            {
                int col = (byte)(P0_data & (1 << j)); 
                int index = i * 8 + j;
                bool value = (row != 0) && (col == 0x00);
                lights[index].enabled = value;
            }
        }
    }
    protected override void OnUpdate()
    {
        base.OnUpdate();
        if (!IsGnd) OnClose();
    }
    protected override void OnClose()
    {
        foreach (var item in lights)
        {
            item.enabled = false;
        }
    }
}
//8位 串行输入 并行输出位移缓存器， 其中并行输出为3态输出， 高、低和高阻抗
// 3 个 IO口输出 8个口
public struct IO_Extension_74HC595 
{
    public byte C595RegisterData;
    private byte tempC595data;
    private int C595RegisterBitIndex;
    private bool SER;
    private bool RCLK;
    private bool SRCLK;

    public void Init() 
    {
        C595RegisterBitIndex = 7;
        tempC595data = 0;
        C595RegisterData = 0;
        SER = true;
        RCLK = true;
        SRCLK = true;
    }

    public bool Excute(bool ser, bool rclk, bool srclk) 
    {
        SER = ser;
        if ((!SRCLK) && srclk) 
        {
            if (C595RegisterBitIndex < 0) C595RegisterBitIndex = 7;
            int servalue = SER? 1 : 0;
            tempC595data |= (byte)(servalue << C595RegisterBitIndex);
            C595RegisterBitIndex--;
        }
        SRCLK = srclk;
        if ((!RCLK) && rclk) 
        {
            C595RegisterData = tempC595data;
            tempC595data = 0;
            C595RegisterBitIndex = 7;
            RCLK = rclk;
            return true;
        }
        RCLK = rclk;
        return false;
    }
}
public class ClickComponent : MonoBehaviour, IPointerClickHandler
{
    public Vector3 moveDis;
    public bool GetState => state;
    private bool state;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!state)
        {
            transform.position += moveDis;
            state = true;
        }
        else
        {
            transform.position -= moveDis;
            state = false;
        }
    }
}


