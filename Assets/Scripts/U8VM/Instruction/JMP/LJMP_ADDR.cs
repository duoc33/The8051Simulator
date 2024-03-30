using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LJMP_ADDR : InstructInfo
{
    public LJMP_ADDR()
    {
        bytes = 3;
        cycles = 2;
        op0_mode = OpType.NONE;
        op1_mode = OpType.NONE;
        opcode_name = "LJMP addr16";
    }
    //��Ҫ��ת�ĵ�ַ ǰ��λ���� ������0��λ�ã������λ���ڲ������ǰ��λ
    //opcode: aaa0 0001   op0: xxxx xxxx  ��ת��ַ:aaa xxxx xxxx 
    public override void exec(_instruct instr)
    {
        VM_8051_Mono.Instance.PC = (ushort)(instr.op0<<8 | instr.op1);
        VM_8051_Mono.Instance.cycles += instr.info.cycles;
    }
}
