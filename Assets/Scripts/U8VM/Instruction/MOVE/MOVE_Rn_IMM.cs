using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MOVE_Rn_IMM : InstructInfo
{
    public MOVE_Rn_IMM()
    {
        bytes = 2;
        cycles = 1;
        op0_mode = OpType.Rn;
        op1_mode = OpType.IMM8;
        opcode_name = "Move Rn,#imm8";
    }
    public override void exec(_instruct instr)
    {
        base.exec(instr);
        //byte RregAddr =(byte)(instr.opcode & 0x07);//0x07 => 0000 0111 �õ����������λ�ĵ�ַ 
        //VM_8051_Mono.Instance.VmWrite(mem_type.RAM, RregAddr, instr.op0);

        //��ȡop1������ src
        ushort data = VM_8051_Mono.Instance.Read_Op(instr,1,mem_type.RAM);
        //������ д�뵽des �������Rn ����opcode�еĵ���λ��
        VM_8051_Mono.Instance.Write_Op(instr, data,0, mem_type.RAM);
    }
}
