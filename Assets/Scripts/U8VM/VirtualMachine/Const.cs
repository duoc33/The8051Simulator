using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Const
{
    public static bool Read_Bit(byte data,int bitIndex) 
    {
        bool res = (data & (1<< bitIndex)) != 0 ? true : false;
        return res;
    }

    public const byte sfr_addr_start = 0x80;
    public const byte ram_bit_addr_start = 0x20;

    public const ushort InterruptVectorAddress_interupt0 = 0x0003;
    public const ushort InterruptVectorAddress_timer0 = 0x000B;
    public const ushort InterruptVectorAddress_interupt1 = 0x0013;
    public const ushort InterruptVectorAddress_timer1 = 0x001B;
    public const ushort InterruptVectorAddress_uart = 0x0023;

    public const  byte A = 0xE0;
    public const byte B = 0xF0;
    public const byte SP = 0x81;
    public const byte DPTR_L = 0x82;
    public const byte DPTR_H = 0x83;

    //����״̬�����⹦�ܼĴ���
    public const byte PSW = 0xD0;
    public const byte PSW_P = 0xD0 + 0; //ACC��1�ĸ���Ϊ����ʱ��1
    public const byte PSW_OV = 0xD0 + 2;
    public const byte PSW_RS0 = 0xD0 + 3;
    public const byte PSW_RS1 = 0xD0 + 4;
    public const byte PSW_F0 = 0xD0 + 5;
    public const byte PSW_AC = 0xD0 + 6;
    public const byte PSW_CY = 0xD0 + 7;

    //P4��
    public const byte P4 = 0xE8;
    // I/O��
    public const byte P0 = 0x80;
    public const byte P1 = 0x90;
    public const byte P2 = 0xA0;
    public const byte P3 = 0xB0;
    public const byte P3_4_T0 = 0xB0+4;//����ⲿ����
    public const byte P3_5_T1 = 0xB0+5;
    public const byte Int0_key3 = 0xB0 + 2;
    public const byte Int1_key4 = 0xB0 + 3;

    public const byte R0 = 0x00;
    public const byte R1 = 0x01;
    public const byte R2 = 0x02;
    public const byte R3 = 0x03;
    public const byte R4 = 0x04;
    public const byte R5 = 0x05;
    public const byte R6 = 0x06;
    public const byte R7 = 0x07;

    //�ж�����Ĵ���
    public const byte IE = 0xA8;
    //������λ
    public const byte IE_EA = 0xA8 + 7;
    //INT0
    public const byte IE_EX0 = 0xA8 + 0;
    //T0
    public const byte IE_ET0 = 0xA8 + 1;
    //INT1
    public const byte IE_EX1 = 0xA8 + 2;
    //T1
    public const byte IE_ET1 = 0xA8 + 3;
    //UART����ͨѶ
    public const byte IE_ES = 0xA8 + 4;
    //�ж����ȼ��Ĵ���
    public const byte IP = 0xB8;
    public const byte IP_PX0 = 0xB8;
    public const byte IP_PT0 = 0xB8 + 1;
    public const byte IP_PX1 = 0xB8 + 2;
    public const byte IP_PT1 = 0xB8 + 3;
    public const byte IP_PS = 0xB8 + 4;
    //���п������⹦�ܼĴ���
    public const byte SCON = 0x98;
    public const byte SCON_TI = 0x98 + 1;
    public const byte SCON_RI = 0x98 + 0;
    public const byte TMOD = 0x89;
    public const byte TMOD_T0_M0 = 0x89;
    public const byte TMOD_T0_M1 = 0x89+1;
    public const byte TMOD_T0_C_T = 0x89+2;
    public const byte TMOD_T0_Gate = 0x89+3;
    public const byte TMOD_T1_M0 = 0x89+4;
    public const byte TMOD_T1_M1 = 0x89+5;
    public const byte TMOD_T1_C_T = 0x89+6;
    public const byte TMOD_T1_Gate = 0x89+7;
    //��ʱ��0�ĸ߰�λ
    public const byte TH0 = 0x8C;//�ǿ�λѰַ�Ĵ���
    //��ʱ��0�ĵͰ�λ
    public const byte TL0 = 0x8A;

    public const byte TH1 = 0x8D;
    public const byte TL1 = 0x8B;
    //TCON ��ʱ��/������ ���ƼĴ���
    public const byte TCON = 0x88;
    public const byte TCON_TR0 = 0x88 + 4;
    public const byte TCON_TR1 = 0x88+6;
    //��ʱ��0��1�Ĵ���
    public const byte TCON_TF0 = 0x88 + 5;
    public const byte TCON_TF1 = 0x88 + 7;
    //�ⲿ�ж�0��1�������ֶ�д��λ
    public const byte TCON_IE0 = 0x88 + 1;
    public const byte TCON_IE1 = 0x88 + 3;
    //�жϴ�����ʽѡ��λ
    public const byte TCON_IT0 = 0x88;
    public const byte TCON_IT1 = 0x88 + 2;

    #region Other
    //�����Ĵ��� xxxx xx00
    public const byte AUXR = 0x8E;
    //�����Ĵ���1
    public const byte AUXR1 = 0xA2;
    //PCON ��Դ���ƼĴ���
    public const byte PCON = 0x87;
    //�ж����ȸ�
    public const byte IPH = 0xB7;
    //�������ݻ������⹦�ܼĴ���
    public const byte SBUF = 0x99;
    //��ʱ��������2
    public const byte T2CON = 0xC8;
    //�����жϿ�����
    public const byte XICON = 0xC0;
    //��ʱ��2ģʽģʽ�Ĵ���
    public const byte T2MOD = 0xC9;
    //���Ź����ƼĴ���
    public const byte WDT = 0xE1;
    //ISP/IAP���ݼĴ���
    public const byte ISP_DATA = 0xE2;
    // ISP/IAP����Ĵ���
    public const byte ISP_CMD = 0xE5;
    //ISP/IAP������Ĵ���
    public const byte ISP_TRIG = 0xE6;
    //ISP/IAP���ƼĴ���
    public const byte ISP_CONTR = 0xE7;
    #endregion

}
