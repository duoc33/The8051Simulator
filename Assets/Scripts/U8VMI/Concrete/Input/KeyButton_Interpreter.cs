using System.Collections;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class KeyButton_Interpreter : VM_Interpreter
{
    private KeyButtonHandler Reset;

    private KeyButtonHandler[] MatrixKeys;
    private bool MatrixKeyFlag = false;
    private byte CurrentMatrixKeyValue;

    private KeyButtonHandler[] IndependenceKeys;
    private bool IndenpendenceKeyFlag = false;
    private byte CurrentRectIndenpendenceKeyValue;

    protected override void OnInit()
    {
        Init();//初始化按键信息类
        AddInputRegisterCheckAddr(Const.P1, WhenReadP1);
        AddInputRegisterCheckAddr(Const.P3, WhenReadP3);
    }
    private void Init() 
    {
        Reset = this.transform.GetChild(0).gameObject.AddComponent<KeyButtonHandler>();
        Reset.UpAction += VMReset;

        MatrixKeys = new KeyButtonHandler[this.transform.GetChild(1).childCount];
        for (int i = 0; i < this.transform.GetChild(1).childCount; i++)
        {
            MatrixKeys[i] = this.transform.GetChild(1).GetChild(i).gameObject.AddComponent<KeyButtonHandler>();
            MatrixKeys[i].keyButtonInfo.Addr = Const.P1;
            MatrixKeys[i].keyButtonInfo.DownValue = (byte)((0xf0 ^ (0x10 << (3 - i / 4))) | (0x0f ^ (0x01 << (3 - i % 4))));
            //Debug.Log("S"+i+" : "+ MatrixKeys[i].keyButtonInfo.DownValue.ToString("X"));
            MatrixKeys[i].keyButtonInfo.UpValue = 0xff;
            MatrixKeys[i].DonwAction += GetRectKeyDown;
            MatrixKeys[i].UpAction += GetRectKeyUp;
        }
        IndependenceKeys = new KeyButtonHandler[this.transform.GetChild(2).childCount];
        for (int i = 0; i < this.transform.GetChild(2).childCount; i++) 
        {
            IndependenceKeys[i] = this.transform.GetChild(2).GetChild(i).gameObject.AddComponent<KeyButtonHandler>();
            IndependenceKeys[i].keyButtonInfo.UpValue = 1;
            IndependenceKeys[i].DonwAction += GetIndependenceKeyDown;
            IndependenceKeys[i].UpAction += GetIndependenceKeyUp;
        }
        IndependenceKeys[0].keyButtonInfo.Addr = Const.P3 + 1;
        IndependenceKeys[0].keyButtonInfo.DownValue = 0xff ^ (0x1 << 1);
        IndependenceKeys[1].keyButtonInfo.Addr = Const.P3 + 0;
        IndependenceKeys[1].keyButtonInfo.DownValue = 0xff ^ (0x1 << 0);
        IndependenceKeys[2].keyButtonInfo.Addr = Const.P3 + 2;
        IndependenceKeys[2].keyButtonInfo.DownValue = 0xff ^ (0x1 << 2);
        IndependenceKeys[3].keyButtonInfo.Addr = Const.P3 + 3;
        IndependenceKeys[3].keyButtonInfo.DownValue = 0xff ^ (0x1 << 3);
    }
    //当矩阵按键有输入时，U8VM虚拟机读P1管脚时，
    private byte WhenReadP1() 
    {
        if (!MatrixKeyFlag) return 0xff;
        else return CurrentMatrixKeyValue;
    }
    private byte WhenReadP3() 
    {
        if (!IndenpendenceKeyFlag) return 0xff;
        else return CurrentRectIndenpendenceKeyValue;
    }
    private void VMReset(KeyButtonInfo info) => VMReset();
    private void GetIndependenceKeyDown(KeyButtonInfo info) 
    {
        IndenpendenceKeyFlag = true;
        CurrentRectIndenpendenceKeyValue = info.DownValue;
    }
    private void GetIndependenceKeyUp(KeyButtonInfo info)
    {
        IndenpendenceKeyFlag = false;
    }
    private void GetRectKeyDown(KeyButtonInfo info)
    {
        MatrixKeyFlag = true;
        CurrentMatrixKeyValue = info.DownValue;
    }
    private void GetRectKeyUp(KeyButtonInfo info)
    {
        MatrixKeyFlag = false;
    }
}
public struct KeyButtonInfo 
{
    public byte Addr;
    public byte DownValue;
    public byte UpValue;
}
public class KeyButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public KeyButtonInfo keyButtonInfo;
    public UnityAction<KeyButtonInfo> DonwAction;
    public UnityAction<KeyButtonInfo> UpAction;
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localPosition = new Vector3(transform.localPosition.x,transform.localPosition.y,0.13f);
        DonwAction?.Invoke(keyButtonInfo);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0.2474185f);
        UpAction?.Invoke(keyButtonInfo);
    }
}
