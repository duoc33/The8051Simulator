
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中断执行方法类
/// </summary>
public class InteruptExcute
{
    private void enter_interupt(ushort addr)
    {
        //进入中断
        VM_8051_Mono.Instance.in_interupt = true;
        //进入中断前存入PC值
        ushort currentInteruptPC = VM_8051_Mono.Instance.PC;
        //得先把中断产生之前的地址压入栈中
        byte sp = VM_8051_Mono.Instance.Read_Sfr(0x81);
        //把PC低八位写入sp对应的内部ram区
        VM_8051_Mono.Instance.Write_Ram(++sp, (byte)(currentInteruptPC & 0xff));
        //把PC高八位写入sp对应的内部ram区
        VM_8051_Mono.Instance.Write_Ram(++sp, (byte)(currentInteruptPC >> 8));
        //写入当前的sp的值进sp
        VM_8051_Mono.Instance.Write_Sfr(0x81, sp);
        VM_8051_Mono.Instance.PC = addr;
    }
    public void execute(InteruptCategory type) 
    {
        switch (type)
        {
            case InteruptCategory.None:
                break;
            case InteruptCategory.Interupt0:
                enter_interupt(Const.InterruptVectorAddress_interupt0);
                VM_8051_Mono.Instance.Write_Bit(Const.TCON_IE0, 1);
                break;
            case InteruptCategory.Interupt1:
                enter_interupt(Const.InterruptVectorAddress_interupt1);
                VM_8051_Mono.Instance.Write_Bit(Const.TCON_IE1, 1);
                break;
            case InteruptCategory.Timer0:
                enter_interupt(Const.InterruptVectorAddress_timer0);
                VM_8051_Mono.Instance.Write_Bit(Const.TCON_TF0, 0);
                break;
            case InteruptCategory.Timer1:
                enter_interupt(Const.InterruptVectorAddress_timer1);
                VM_8051_Mono.Instance.Write_Bit(Const.TCON_TF1, 0);
                break;
            case InteruptCategory.Uart:
                enter_interupt(Const.InterruptVectorAddress_uart);
                break;
        }
    }
}
/// <summary>
/// 中断类型
/// </summary>
public enum InteruptCategory
{
    None,
    Interupt0,
    Interupt1,
    Timer0,
    Timer1, 
    Uart
}

public struct InteruptInfo 
{
    public InteruptCategory type;
    public int priority;
    public bool IsHighPriority;
}

public class VM_InteruptSystem 
{

    #region 中断触发方式选择位
    private byte lastInt0 = 1;
    private byte lastInt1 = 1;
    private bool mode0 = false;//true 下降沿，false 低电平触发
    private bool mode1 = false;
    #endregion

    private InteruptInfo int0Info = new InteruptInfo() { type = InteruptCategory.Interupt0, priority = 0 };
    private InteruptInfo timer0Info = new InteruptInfo() { type = InteruptCategory.Timer0, priority = 1 };
    private InteruptInfo int1Info = new InteruptInfo() { type = InteruptCategory.Interupt1, priority = 2 };
    private InteruptInfo timer1Info = new InteruptInfo() { type = InteruptCategory.Timer1, priority = 3 };
    private InteruptInfo uartInfo = new InteruptInfo() { type = InteruptCategory.Uart, priority = 4 };
    private InteruptExcute enterInterupt = new InteruptExcute();
    private List<InteruptInfo> interuptInfos = new List<InteruptInfo>();
    //当前正在执行的中断
    private InteruptInfo currentInterupt;
    private void CheckAndAddInterupts() 
    {
        int0Info.IsHighPriority = VM_8051_Mono.Instance.Read_Bit(Const.IP_PX0) == 1;
        timer0Info.IsHighPriority = VM_8051_Mono.Instance.Read_Bit(Const.IP_PT0) == 1;
        int1Info.IsHighPriority = VM_8051_Mono.Instance.Read_Bit(Const.IP_PX1) == 1;
        timer1Info.IsHighPriority = VM_8051_Mono.Instance.Read_Bit(Const.IP_PT1) == 1;
        uartInfo.IsHighPriority = VM_8051_Mono.Instance.Read_Bit(Const.IP_PS) == 1;

        byte int0 = VM_8051_Mono.Instance.Read_Bit(Const.Int0_key3);
        byte ex0 = VM_8051_Mono.Instance.Read_Bit(Const.IE_EX0);
        byte int1 = VM_8051_Mono.Instance.Read_Bit(Const.Int1_key4);
        byte ex1 = VM_8051_Mono.Instance.Read_Bit(Const.IE_EX1);
        mode0 = VM_8051_Mono.Instance.Read_Bit(Const.TCON_IT0) == 1 ? true : false;
        mode1 = VM_8051_Mono.Instance.Read_Bit(Const.TCON_IT1) == 1 ? true : false;
        if (ex0 == 1 &&((!mode0 && int0 == 0) || (mode0 && lastInt0 == 1 && int0 == 0)))
        {
            interuptInfos.Add(int0Info);
        }
        if (VM_8051_Mono.Instance.Read_Bit(Const.IE_ET0) == 1 &&
            VM_8051_Mono.Instance.Read_Bit(Const.TCON_TF0) == 1)
        {
            interuptInfos.Add(timer0Info);
        }
        if (ex1 == 1&& ((!mode1 && int1 == 0) || (mode1 && lastInt1 == 1 && int1 == 0)))
        {
            interuptInfos.Add(int1Info);
        }
        if (VM_8051_Mono.Instance.Read_Bit(Const.IE_ET1) == 1&& 
            VM_8051_Mono.Instance.Read_Bit(Const.TCON_TF1) == 1)
        {
            interuptInfos.Add(timer1Info);
        }
        if (VM_8051_Mono.Instance.Read_Bit(Const.IE_ES) == 1&&
            (VM_8051_Mono.Instance.Read_Bit(Const.SCON_RI) == 1 ||
         VM_8051_Mono.Instance.Read_Bit(Const.SCON_TI) == 1))
        {
            interuptInfos.Add(uartInfo);
        }

        lastInt0 = int0;
        lastInt1 = int1;
    }

    private void ResetOrder() 
    {
        if (interuptInfos.Count == 0) return;
        interuptInfos.Sort((x, y) =>
        {
            // 首先比较IsHighPriority，true在前
            int highPriorityComparison = y.IsHighPriority.CompareTo(x.IsHighPriority);
            if (highPriorityComparison != 0)
            {
                return highPriorityComparison;
            }
            // IsHighPriority相同，根据priority排序，数值小的在前
            return x.priority.CompareTo(y.priority);
        });
    }
    private void Excute() 
    {
        if (interuptInfos.Count == 0) return;
        if (VM_8051_Mono.Instance.in_interupt)
        {
            InteruptInfo newInfo = interuptInfos[0];
            //如果新的中断请求IP相关的位设置为0，当前的IP相关的位设置为1，则返回，不执行新的中断请求
            if ((!newInfo.IsHighPriority) && currentInterupt.IsHighPriority) return;
            //如果新的中断请求IP寄存器相关位的优先级设置一样，则比较它们的自然优先级，
            //新的中断请求自然优先级小与等于当前的中断请求，直接返回不执行。
            if (newInfo.IsHighPriority == currentInterupt.IsHighPriority && newInfo.priority >= currentInterupt.priority) return;
        }
        //执行最高优先级的中断
        currentInterupt = interuptInfos[0];
        enterInterupt.execute(interuptInfos[0].type);
        interuptInfos.RemoveAt(0);
    }
    public void InteruptDetect()
    {
        //判断中断总允许位
        if (VM_8051_Mono.Instance.Read_Bit(Const.IE_EA) != 1) return;
        CheckAndAddInterupts();
        ResetOrder();
        Excute();
    }
    public void Reset() => interuptInfos.Clear();
}

