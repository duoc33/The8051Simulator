using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MUL_AB : InstructInfo
{
    public MUL_AB() 
    { 
        bytes = 1;
        cycles = 4;
        op0_mode = OpType.ACC;
        op1_mode = OpType.B;
        opcode_name = "MUL AB";
    }
    public override void exec(_instruct instr)
    {
        ushort acc = VM_8051_Mono.Instance.Read_Op(instr,0,mem_type.RAM);
        ushort b = VM_8051_Mono.Instance.Read_Op(instr,1,mem_type.RAM);
        ushort result = (ushort)(acc * b);
        //set OV CY ��־λ
        //OV ��� ������ һ���ֽ�
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT,0xD2, (byte)(result >255?1:0));
        //CY ����Ҫ����
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT,0xD7,0);

        //�߰�λд�� ��һ��������(ACC)
        VM_8051_Mono.Instance.Write_Op(instr, (ushort)(result & 0xff), 0, mem_type.RAM);
        //�Ͱ�λд�� �ڶ���������(B)
        VM_8051_Mono.Instance.Write_Op(instr, (ushort)(result >> 8), 1,mem_type.RAM);
        base.exec(instr);
    }
}
