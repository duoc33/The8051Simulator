using System.Collections.Generic;
using System.Threading;

public class VM_Timer 
{
    #region Timer0
    //Timer 0 Gate 位
    private byte Timer0_TMOD_GATE => Read_Bit(Const.TMOD_T0_Gate);
    //Timer 0 在Gate==1时P3.3位必须为高电平 TRO =1
    private byte Timer0_EA_Int0 => Read_Bit(Const.Int0_key3);
    //Timer 0 计数器/定时器 选择位
    private byte Timer0_TMOD_C_T => Read_Bit(Const.TMOD_T0_C_T);
    // Timer 0  工作方式选择
    private byte Timer0_TMOD_MODE => (byte)(Read_Sfr(Const.TMOD)&0x3);
    //Timer 0 计数器模式下的脉冲信号接收位
    private byte Timer0_Impulse_Signal => Read_Bit(Const.P3_4_T0);
    //Timer 0 启动位
    private byte Timer0_TRO => Read_Bit(Const.TCON_TR0);
    //Timer 0 溢出位
    private byte Timer0_TF0 { set { Write_Bit(Const.TCON_TF0, value); } }
    private byte Timer0_TH0 {
        get { return Read_Sfr(Const.TH0); }
        set { Write_Sfr(Const.TH0, value); } 
    }
    private byte Timer0_TL0 {
        get { return Read_Sfr(Const.TL0); }
        set { Write_Sfr(Const.TL0, value); } 
    }
    private byte Timer0_Last_Impulse_Signal;
    #endregion

    #region Timer1
    //Timer 1 Gate 位
    private byte Timer1_TMOD_GATE => Read_Bit(Const.TMOD_T1_Gate);
    //Timer 0 在Gate==1时P3.4位必须为高电平,TR1 =1
    private byte Timer1_EA_Int1 => Read_Bit(Const.Int1_key4);
    //Timer 1 计数器/定时器 选择位
    private byte Timer1_TMOD_C_T => Read_Bit(Const.TMOD_T1_C_T);
    // Timer 1  工作方式选择
    private byte Timer1_TMOD_MODE=> (byte)((Read_Sfr(Const.TMOD) & 0x30) >>4);
    //Timer 1 计数器模式下的脉冲信号接收位
    private byte Timer1_Impulse_Signal => Read_Bit(Const.P3_5_T1);
    //Timer 1 启动位
    private byte Timer1_TR1 => Read_Bit(Const.TCON_TR1);
    //Timer 1 溢出位
    private byte Timer1_TF1{ set { Write_Bit(Const.TCON_TF1,value); }  }
    private byte Timer1_TH1 {
        get { return Read_Sfr(Const.TH1); }
        set { Write_Sfr(Const.TH1, value); }
    }
    private byte Timer1_TL1 {
        get { return Read_Sfr(Const.TL1); }
        set { Write_Sfr(Const.TL1, value); } 
    }
    private byte Timer1_Last_Impulse_Signal;
    #endregion

    private int count = 0;//计数

    private void Timer0Check(int cycles) //指令机器周期
    {
        byte gate = Timer0_TMOD_GATE;//Gate 位
        byte tr0 = Timer0_TRO; //tr0位
        //gate =0 ，tr0 ==1 和 gate=1 ，tr0=1，外部中断0高电平，满足其中一个条件定时器/计数器0开启
        if ((gate == 0&& tr0 == 1) || (gate == 1 && tr0 == 1 && Timer0_EA_Int0 == 1))
        {
            //执行定时功能
            if (Timer0_TMOD_C_T == 0)
            {
                Execute(cycles);
            }
            else //执行计数功能,模拟脉冲信号,用上一次信号，与当前信号比较
            {
                
                if (Timer0_Last_Impulse_Signal == 0 && Timer1_Impulse_Signal == 1) 
                {
                    Execute(1);
                }
                Timer0_Last_Impulse_Signal = Timer1_Impulse_Signal;
            }
        }
    }
    private void Execute(int cycles)
    {
        int count;
        switch (Timer0_TMOD_MODE)
        {
            case 0x00:
                //获取TH0，及TL0的低5位的组合值
                count = (Timer0_TH0 << 5) | (Timer0_TL0 & 0x1F);
                count += cycles;
                if (count > 0x1fff) //0xb 0001 1111 1111 1111
                {
                    //溢出，TF0标志位置1
                    Timer0_TF0 = 1;
                    count = 0;
                }
                //当前count大小，高八位写入TH0，低5位写入TL0
                Timer0_TH0 = (byte)(count >> 5);
                Timer0_TL0 = (byte)(count & 0x1F);
                break;
            case 0x01:
                //获取TH0，TL0的组合值
                count = (Timer0_TH0 << 8) | Timer0_TL0;
                count += cycles;
                if (count > 0xffff)
                {
                    //溢出，TF0标志位置1
                    Timer0_TF0 = 1;
                    count = 0;
                }
                //当前count大小，写入TH0,TL0寄存器
                Timer0_TH0 = (byte)(count >> 8);
                Timer0_TL0 = (byte)(count & 0xFF);
                break;
            case 0x10:
                //获取TL0的值
                count = Timer0_TL0&0xff;
                count += cycles;
                if (count > 0xff) 
                {
                    //重新装填初始值
                    Timer0_TL0 = Timer0_TH0;
                    count = 0;
                }
                Timer0_TL0 = (byte)(count & 0xff);
                break;
            case 0x11:
                //TH0寄存器，只能用在定时器功能上
                if (Timer0_TMOD_C_T == 0) 
                {
                    count = Timer0_TH0 & 0xff;
                    count += cycles;
                    if (count > 0xff)
                    {
                        Timer1_TF1 = 1;//定时器1溢出标志位置1，这也是为什么定时1没有模式3的原因
                        count = 0;
                    }
                    Timer0_TH0 = (byte)(count & 0xff);
                }
                //TL0寄存器则都可以
                count = Timer0_TL0 & 0xff;
                count += cycles;
                if (count > 0xff) 
                {
                    Timer0_TF0 = 1;//定时器0溢出标志位置1
                    count = 0;
                }
                Timer0_TL0 = (byte)(count & 0xff);

                break;
             default:
                break;
        }
    }
    public void Update_timer(int Cycles)
    {
        if (Read_Bit(Const.TMOD_T0_C_T) != 0) return;
        byte gate0 = Read_Bit(Const.TMOD_T0_Gate);
        if (gate0 != 0 && Read_Bit(Const.Int0_key3) != 1) return; 

        //读取定时器0的TR0位
        byte tr0 = Read_Bit(Const.TCON_TR0);
        //定时器启动，TR0置1才启动定时器
        if (tr0 != 0)
        {
            //读取定时器工作模式
            byte tmod = Read_Sfr(Const.TMOD);
            switch (tmod & 0x3)
            {
                case 0x00:
                    break;
                //定时器16位的工作模式
                case 0x01:
                    {
                        count = (Read_Sfr(Const.TH0) << 8) | Read_Sfr(Const.TL0);
                        count += Cycles;
                        if (count > 0xffff)
                        {
                            Write_Bit(Const.TCON_TF0, 1);
                            count = 0;
                        }
                        Write_Sfr(Const.TH0, (byte)(count >> 8));
                        Write_Sfr(Const.TL0, (byte)(count & 0xFF));
                        break;
                    }
                case 0x02:
                    break;
                case 0x03:
                    break;
                default:
                    break;
            }
        }
    }
    public void GetTimerTriggerMode() 
    {
        if (Read_Bit(Const.TMOD_T0_C_T) != 0) return;
        byte gate0 = Read_Bit(Const.TMOD_T0_Gate);
        if (gate0 != 0 && Read_Bit(Const.Int0_key3) != 1) return;
    }


    private byte Read_Bit(byte addr) => VM_8051_Mono.Instance.Read_Bit(addr);
    private void Write_Bit(byte addr,int bit) => VM_8051_Mono.Instance.Write_Bit(addr,bit);
    private byte Read_Sfr(byte addr) => VM_8051_Mono.Instance.Read_Sfr(addr);
    private void Write_Sfr(byte addr, byte data) => VM_8051_Mono.Instance.Write_Sfr(addr, data);
}
