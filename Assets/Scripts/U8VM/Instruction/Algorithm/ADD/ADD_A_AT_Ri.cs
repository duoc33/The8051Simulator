using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;

public class ADD_A_AT_Ri : InstructInfo
{
    public ADD_A_AT_Ri()
    {
        bytes = 1;
        cycles = 1;
        op0_mode = OpType.ACC;
        op1_mode = OpType.R0_R1;
        opcode_name = "ADD A , @Ri";
    }

    public override void exec(_instruct instr)
    {
        ushort src = VM_8051_Mono.Instance.Read_Op(instr, 1, mem_type.RAM);
        ushort acc = VM_8051_Mono.Instance.Read_Op(instr, 0, mem_type.RAM);
        ushort result = (ushort)(src + acc);
        VM_8051_Mono.Instance.Write_Op(instr, result, 0, mem_type.RAM);
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD7, (byte)((result & 0x100) != 0 ? 1 : 0));
        if ((((src & 0x0F) + (acc & 0x0F)) & 0x10) != 0)
        {
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD6, 0x1);
        }
        else
        {
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD6, 0x0);
        }
        int bit_7 = result >= 0x100 ? 1 : 0;
        int bit_6 = (acc & 0x7f) + (src & 0x7f) >= 0x80 ? 1 : 0;
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD2, (byte)(bit_6 ^ bit_7));
        base.exec(instr);
    }
    private void UpdateFlag(ushort src,ushort acc, ushort result) 
    {
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD7, (byte)((result & 0x100) != 0 ? 1 : 0));
        if ((((src & 0x0F) + (acc & 0x0F)) & 0x10) != 0)
        {
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD6, 0x1);
        }
        else
        {
            VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD6, 0x0);
        }
        int bit_7 = result >= 0x100 ? 1 : 0;
        int bit_6 = (acc & 0x7f) + (src & 0x7f) >= 0x80 ? 1 : 0;
        VM_8051_Mono.Instance.VmWrite(mem_type.BIT, 0xD2, (byte)(bit_6 ^ bit_7));
    }
}


