using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Led_Interpreter : VM_Interpreter
{
    private byte P2_Data;
    private const byte P2_Addr = Const.P2;
    private byte[] Leds;
    protected override void OnInit()
    {
        Leds = new byte[8] {0x1,0x2,0x4,0x8,0x10,0x20,0x40,0x80};
        AddRegisterCheckAddr(P2_Addr);
    }
    protected override void OnStateChanged(float elapsedtime)
    {
        if (TryGetData(P2_Addr,out byte result))
        {
            P2_Data = result;
        }
        Excute(P2_Data);
        Debug.Log(elapsedtime);
    }
    protected override void OnClose() 
    {
        base.OnClose();
        for (int i = 0; i < Leds.Length; i++)
        {
            Material mat = transform.GetChild(i).GetComponent<MeshRenderer>().material;
            mat.SetColor("_Color", Color.white);
        }
    }
    private void Excute(byte P2_Data)
    {
        for (int i = 0; i < Leds.Length; i++)
        {
            bool ledState = (Leds[i] & P2_Data) == 0 ? true : false;
            Material mat = transform.GetChild(i).GetComponent<MeshRenderer>().material;
            if (ledState)
            {
                mat.SetColor("_Color", Color.red);
            }
            else
            {
                mat.SetColor("_Color", Color.white);
            }
        }
    }
}
