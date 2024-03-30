using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DIV_AB : InstructInfo
{
    public DIV_AB() 
    {
        bytes = 1;
        cycles = 4;
        op0_mode = OpType.ACC;
        op1_mode = OpType.B;
        opcode_name = "DIV AB";
    }
    public override void exec(_instruct instr)
    {
        ushort acc = VM_8051_Mono.Instance.Read_Op(instr, 0, mem_type.RAM);
        ushort b = VM_8051_Mono.Instance.Read_Op(instr, 1, mem_type.RAM);
        if (b == 0)//����Ϊ0
        {
            //ov����Ϊ1
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT,0xD2,1);
        }
        else
        {
            //���� �Ž�0�żĴ��� ACC
            VM_8051_Mono.Instance.Write_Op(instr, (ushort)(acc/b),0,mem_type.RAM);
            //���� �Ž� 1 �żĴ��� B
            VM_8051_Mono.Instance.Write_Op(instr, (ushort)(acc%b), 1, mem_type.RAM);
            //OV ����
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD2, 0);
        }
        //����ʲô���CYλдΪ0 
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD7, 0);
        base.exec(instr);
    }
}
