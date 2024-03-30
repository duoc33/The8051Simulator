using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SJMP : InstructInfo
{
    public SJMP() 
    {
        bytes = 2;
        cycles =2;
        op0_mode=OpType.NONE;
        op1_mode=OpType.NONE;
        opcode_name = "SJMP rel";
    }
    public override void exec(_instruct instr)
    {
        base.exec(instr);
        //SJMP�Ĳ��������ǰѵ�ǰPCֵ ���ϲ�����0(op0)��ֵ,�õ�PC��ת��λ��
        VM_8051_Mono.Instance.PC += (ushort)(sbyte)instr.op0;//����һ���з���������Ҫ��byteת������
    }
}
