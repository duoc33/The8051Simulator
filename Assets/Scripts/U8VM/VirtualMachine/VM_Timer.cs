using System.Collections.Generic;
using System.Threading;

public class VM_Timer 
{
    #region Timer0
    //Timer 0 Gate λ
    private byte Timer0_TMOD_GATE => Read_Bit(Const.TMOD_T0_Gate);
    //Timer 0 ��Gate==1ʱP3.3λ����Ϊ�ߵ�ƽ TRO =1
    private byte Timer0_EA_Int0 => Read_Bit(Const.Int0_key3);
    //Timer 0 ������/��ʱ�� ѡ��λ
    private byte Timer0_TMOD_C_T => Read_Bit(Const.TMOD_T0_C_T);
    // Timer 0  ������ʽѡ��
    private byte Timer0_TMOD_MODE => (byte)(Read_Sfr(Const.TMOD)&0x3);
    //Timer 0 ������ģʽ�µ������źŽ���λ
    private byte Timer0_Impulse_Signal => Read_Bit(Const.P3_4_T0);
    //Timer 0 ����λ
    private byte Timer0_TRO => Read_Bit(Const.TCON_TR0);
    //Timer 0 ���λ
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
    //Timer 1 Gate λ
    private byte Timer1_TMOD_GATE => Read_Bit(Const.TMOD_T1_Gate);
    //Timer 0 ��Gate==1ʱP3.4λ����Ϊ�ߵ�ƽ,TR1 =1
    private byte Timer1_EA_Int1 => Read_Bit(Const.Int1_key4);
    //Timer 1 ������/��ʱ�� ѡ��λ
    private byte Timer1_TMOD_C_T => Read_Bit(Const.TMOD_T1_C_T);
    // Timer 1  ������ʽѡ��
    private byte Timer1_TMOD_MODE=> (byte)((Read_Sfr(Const.TMOD) & 0x30) >>4);
    //Timer 1 ������ģʽ�µ������źŽ���λ
    private byte Timer1_Impulse_Signal => Read_Bit(Const.P3_5_T1);
    //Timer 1 ����λ
    private byte Timer1_TR1 => Read_Bit(Const.TCON_TR1);
    //Timer 1 ���λ
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

    private int count = 0;//����

    private void Timer0Check(int cycles) //ָ���������
    {
        byte gate = Timer0_TMOD_GATE;//Gate λ
        byte tr0 = Timer0_TRO; //tr0λ
        //gate =0 ��tr0 ==1 �� gate=1 ��tr0=1���ⲿ�ж�0�ߵ�ƽ����������һ��������ʱ��/������0����
        if ((gate == 0&& tr0 == 1) || (gate == 1 && tr0 == 1 && Timer0_EA_Int0 == 1))
        {
            //ִ�ж�ʱ����
            if (Timer0_TMOD_C_T == 0)
            {
                Execute(cycles);
            }
            else //ִ�м�������,ģ�������ź�,����һ���źţ��뵱ǰ�źűȽ�
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
                //��ȡTH0����TL0�ĵ�5λ�����ֵ
                count = (Timer0_TH0 << 5) | (Timer0_TL0 & 0x1F);
                count += cycles;
                if (count > 0x1fff) //0xb 0001 1111 1111 1111
                {
                    //�����TF0��־λ��1
                    Timer0_TF0 = 1;
                    count = 0;
                }
                //��ǰcount��С���߰�λд��TH0����5λд��TL0
                Timer0_TH0 = (byte)(count >> 5);
                Timer0_TL0 = (byte)(count & 0x1F);
                break;
            case 0x01:
                //��ȡTH0��TL0�����ֵ
                count = (Timer0_TH0 << 8) | Timer0_TL0;
                count += cycles;
                if (count > 0xffff)
                {
                    //�����TF0��־λ��1
                    Timer0_TF0 = 1;
                    count = 0;
                }
                //��ǰcount��С��д��TH0,TL0�Ĵ���
                Timer0_TH0 = (byte)(count >> 8);
                Timer0_TL0 = (byte)(count & 0xFF);
                break;
            case 0x10:
                //��ȡTL0��ֵ
                count = Timer0_TL0&0xff;
                count += cycles;
                if (count > 0xff) 
                {
                    //����װ���ʼֵ
                    Timer0_TL0 = Timer0_TH0;
                    count = 0;
                }
                Timer0_TL0 = (byte)(count & 0xff);
                break;
            case 0x11:
                //TH0�Ĵ�����ֻ�����ڶ�ʱ��������
                if (Timer0_TMOD_C_T == 0) 
                {
                    count = Timer0_TH0 & 0xff;
                    count += cycles;
                    if (count > 0xff)
                    {
                        Timer1_TF1 = 1;//��ʱ��1�����־λ��1����Ҳ��Ϊʲô��ʱ1û��ģʽ3��ԭ��
                        count = 0;
                    }
                    Timer0_TH0 = (byte)(count & 0xff);
                }
                //TL0�Ĵ����򶼿���
                count = Timer0_TL0 & 0xff;
                count += cycles;
                if (count > 0xff) 
                {
                    Timer0_TF0 = 1;//��ʱ��0�����־λ��1
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

        //��ȡ��ʱ��0��TR0λ
        byte tr0 = Read_Bit(Const.TCON_TR0);
        //��ʱ��������TR0��1��������ʱ��
        if (tr0 != 0)
        {
            //��ȡ��ʱ������ģʽ
            byte tmod = Read_Sfr(Const.TMOD);
            switch (tmod & 0x3)
            {
                case 0x00:
                    break;
                //��ʱ��16λ�Ĺ���ģʽ
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
