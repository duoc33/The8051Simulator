using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RET : InstructInfo
{
    public RET()
    {
        bytes = 1;
        cycles = 2;
        op0_mode = OpType.NONE;
        op1_mode = OpType.NONE;
        opcode_name = "RET";
    }
    public override void exec(_instruct instr)
    {
        ushort ret_addr;
        byte sp = VM_8051_Mono.Instance.Read_Sfr(0x81);
        //��һ�ζ�ȡ��sp��ֵ �õ��ķ��صĵ�ֵַΪ�߰�λ
        ret_addr = (ushort)(VM_8051_Mono.Instance.Read_Ram(sp--)<<8);
        //��һ�ζ�ȡ��sp��ֵ �õ��ķ��صĵ�ֵַΪ�Ͱ�λ����������ֵ���������õ����ص�ַ
        ret_addr |= VM_8051_Mono.Instance.Read_Ram(sp--);
        //spֵд��ȥ
        VM_8051_Mono.Instance.Write_Sfr(0x81, sp);
        //����PCֵ
        VM_8051_Mono.Instance.PC = ret_addr;
        VM_8051_Mono.Instance.cycles+= instr.info.cycles;
    }
}
