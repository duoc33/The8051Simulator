using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RLC_A : InstructInfo
{
    public RLC_A()
    {
        bytes = 1;
        cycles = 1;
        op0_mode = OpType.ACC;
        op1_mode = OpType.NONE;
        opcode_name = "RLC A";
    }

    public override void exec(_instruct instr)
    {
        ushort data = VM_8051_Mono.Instance.Read_Op(instr, 0, mem_type.RAM);
        byte c = VM_8051_Mono.Instance.VmRead(mem_type.BIT, 0xD7);//CY��λ ����A�ĵ�һλ
        byte A_MH = (byte)(data >> 7);//A �����λ����CY
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD7, A_MH);//����CYλ��ֵλA�����λ
        data = (ushort)(c | (data << 1));//A��ֵΪ ����1λ������CYλ��ԭʼֵ������һλ
        VM_8051_Mono.Instance.Write_Op(instr,data,0,mem_type.RAM);
        base.exec(instr);
    }
}
