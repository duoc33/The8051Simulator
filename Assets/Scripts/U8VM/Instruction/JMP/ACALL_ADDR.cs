using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACALL_ADDR : InstructInfo
{   
    public ACALL_ADDR() 
    {
        bytes = 2;
        cycles = 2;
        op0_mode = OpType.NONE;
        op1_mode = OpType.NONE;
        opcode_name = "ACALL addr11";
    }
    public override void exec(_instruct instr)
    {
        VM_8051_Mono.Instance.PC += instr.info.bytes;

        //ȡsp��ֵ,������һ���ڲ�RAM��ַ����1
        byte sp = (byte)(VM_8051_Mono.Instance.VmRead(mem_type.SFR, 0x81)+1);
        //ȡPC�ĵͰ�λ��д��sp��ȡ�õ�sp+1�ĵ�ַ��
        VM_8051_Mono.Instance.VmWrite(mem_type.RAM, sp++, (byte)(VM_8051_Mono.Instance.PC &0xFF));
        ///ȡPC�ĸ߰�λ��д��sp��ȡ�õ�sp+2�ĵ�ַ��
        VM_8051_Mono.Instance.VmWrite(mem_type.RAM, sp, (byte)(VM_8051_Mono.Instance.PC>>8));
        //��spֵд��sp
        VM_8051_Mono.Instance.VmWrite(mem_type.SFR, 0x81, sp);

        //PC��ֵ�仯 1111 1000 0000 0000  E0=1110 0000
        ushort temp = (ushort)(VM_8051_Mono.Instance.PC & 0xF800);
        ushort temp1 = (ushort)(((instr.opcode & 0xE0) << 3) | instr.op0);
        VM_8051_Mono.Instance.PC = (ushort)(temp | temp1);

        VM_8051_Mono.Instance.cycles += instr.info.cycles;
    }
}
