using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SUBB_BASE : InstructInfo
{
    public SUBB_BASE(ushort Bytes, int Cycles, OpType op0, OpType op1, string Opcode_name) : base()
    {
        bytes = Bytes;
        cycles = Cycles;
        op0_mode = op0;
        op1_mode = op1;
        opcode_name = Opcode_name;
    }

    public override void exec(_instruct instr)
    {
        ushort src = VM_8051_Mono.Instance.Read_Op(instr, 1, mem_type.RAM);
        src += VM_8051_Mono.Instance.VmRead(mem_type.BIT,0xD7);//����CY��־λ��ֵ
        ushort acc = VM_8051_Mono.Instance.Read_Op(instr, 0, mem_type.RAM);

        ushort result = (ushort)(acc-src);
        VM_8051_Mono.Instance.Write_Op(instr, result, 0, mem_type.RAM);

        //����������־λCY OV AC ��ǰ��λ
        //���acc����������CY��1
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD7, (byte)((acc < src) ? 1 : 0));
        //���ACC����λ ������src����λ����AC��1
        if ((acc & 0x0F) < (src & 0x0F))
        {
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD6, 0x1);
        }
        else
        {
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD6, 0x0);
        }
        //OV ��� ����λ��������acc-src<0 ���ߣ�����λ���µĲ����� ����ֻ��һ������ʱ����1
        int bit_7 = (acc < src) ? 1 : 0;
        int bit_6 = (acc & 0x7f) < (src & 0x7f)?1:0;
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD2, (byte)(bit_6 ^ bit_7));
        base.exec(instr);
    }
}
