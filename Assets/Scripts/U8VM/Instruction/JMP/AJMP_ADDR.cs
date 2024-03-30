using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AJMP_ADDR : InstructInfo
{
    public AJMP_ADDR() 
    {
        bytes = 2;
        cycles = 2;
        op0_mode = OpType.NONE;
        op1_mode = OpType.NONE;
        opcode_name = "AJMP addr";
    }
    //��Ҫ��ת�ĵ�ַ ǰ��λ���� ������0��λ�ã������λ���ڲ������ǰ��λ
    //opcode: aaa0 0001   op0: xxxx xxxx  ��ת��ַ:aaa xxxx xxxx 
    public override void exec(_instruct instr)
    {
        VM_8051_Mono.Instance.PC += 2;
        //0xf800 = 1111 1000 0000 0000 & PC =������PC�ĸ�5λ����11λ����
        //   instr.op0 | ((instr.opcode & 0xE0) << 3)  0xE0 = 1110 0000 ��������λ��������3λ���8~11λ
        ushort temp = (ushort)(VM_8051_Mono.Instance.PC & 0xF800);
        ushort temp1 = (ushort)(((instr.opcode & 0xE0) << 3) | instr.op0);
        VM_8051_Mono.Instance.PC= (ushort)(temp | temp1);
        VM_8051_Mono.Instance.cycles += instr.info.cycles;
    }
}
