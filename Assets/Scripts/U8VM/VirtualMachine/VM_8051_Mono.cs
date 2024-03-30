using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
public delegate void SFROutputCheck(byte[] sfr_ram);
public delegate byte SFRInputCheck();
public class VM_8051_Mono
{
    #region data transfer
    private Dictionary<byte, SFRInputCheck> InputCheckDelegateDic;
    public event SFROutputCheck DataCheckEvent;
    public void AddInputData(byte addr, byte value, bool isBit)
    {
        InputDataQueue.Enqueue(new SFRInputData() { addr = addr, value = value, IsBit = isBit });
    }
    private struct SFRInputData
    {
        public bool IsBit;
        public byte addr;
        public byte value;
    }

    private ConcurrentQueue<SFRInputData> InputDataQueue = new ConcurrentQueue<SFRInputData>();
    private void InputCheck()
    {
        while (InputDataQueue.Count > 0)
        {
            if (InputDataQueue.TryDequeue(out SFRInputData data))
            {
                if (data.IsBit) 
                {
                    Write_Bit(data.addr,data.value);
                }
                else
                {
                    Write_Sfr(data.addr, data.value);
                }
            }
        }
    }
    private void OutputCheck()
    {
        DataCheckEvent?.Invoke(sfr_ram);
    }

    private byte GetInputCheckData(byte addr)
    {
        byte inputValue = 0xff;
        if (InputCheckDelegateDic.ContainsKey(addr))
        {
            if (InputCheckDelegateDic[addr] != null)
            {
                inputValue = InputCheckDelegateDic[addr].Invoke();
            }
        }
        return inputValue;
    }
    public void RegisterInputCheckEvent(byte addr, SFRInputCheck InputReturnAction)
    {
        if (InputCheckDelegateDic == null) InputCheckDelegateDic = new Dictionary<byte, SFRInputCheck>();
        if (InputCheckDelegateDic.ContainsKey(addr)) return;
        InputCheckDelegateDic.Add(addr, InputReturnAction);
    }
    #endregion

    #region 初始化
    private static VM_8051_Mono instance;
    public static VM_8051_Mono Instance
    {
        get
        {
            if (instance == null) instance = new VM_8051_Mono();
            return instance;
        }
    }
    private VM_8051_Mono()
    {
        rom = new byte[65536];
        external_ram = new byte[65536];
        sfr_ram = new byte[256];
        opcodeDic = new Dictionary<byte, InstructInfo>();
        opcodeDic.Clear();
        instr = new _instruct();
        vm_Interupt = new VM_InteruptSystem();
        vM_Timer = new VM_Timer();
        Init_VM();
    }
    private void Init_VM()
    {
        AddDic();
        // Debug.Log(opcodeDic.Count);
        Reset();
        
    }
    public void Reset()
    {
        InputDataQueue.Clear();
        vm_Interupt.Reset();
        PC = 0x0000;
        cycles = 0;
        in_interupt = false;
        byte auxr = (byte)(sfr_ram[Const.AUXR] & 0xFC);
        byte auxr1 = (byte)(sfr_ram[Const.AUXR1] & 0xF6);
        byte sbuf = sfr_ram[Const.SBUF];
        byte pcon = (byte)((sfr_ram[Const.PCON] & 0x30) | 0x10);
        byte ie = (byte)(sfr_ram[Const.IE] & 0x40);
        byte ip = (byte)(sfr_ram[Const.IP] & 0xC0);
        byte t2mod = (byte)(sfr_ram[Const.T2MOD] & 0xFC);
        byte wdt = (byte)(sfr_ram[Const.WDT] & 0xC0);
        byte isp_cmd = (byte)(sfr_ram[Const.ISP_CMD] & 0xF8);
        byte isp_trig = sfr_ram[Const.ISP_TRIG];
        byte isp_contr = (byte)(sfr_ram[Const.ISP_CONTR] & 0x18);


        Array.Clear(external_ram, 0, external_ram.Length);
        Array.Clear(sfr_ram, 0, sfr_ram.Length);

        Write_Sfr(Const.P0, 0xFF);
        Write_Sfr(Const.P1, 0xFF);
        Write_Sfr(Const.P2, 0xFF);
        Write_Sfr(Const.P3, 0xFF);
        Write_Sfr(Const.ISP_DATA, 0xFF);//ISP/IAP 数据寄存器
        Write_Sfr(Const.AUXR, auxr);
        Write_Sfr(Const.AUXR1, auxr1);
        Write_Sfr(Const.SBUF, sbuf);
        Write_Sfr(Const.PCON, pcon);
        Write_Sfr(Const.IE, ie);
        Write_Sfr(Const.IP, ip);
        Write_Sfr(Const.T2MOD, t2mod);
        Write_Sfr(Const.WDT, wdt);
        Write_Sfr(Const.ISP_CMD, isp_cmd);
        Write_Sfr(Const.ISP_TRIG, isp_trig);
        Write_Sfr(Const.ISP_CONTR, isp_contr);
        Write_Sfr(Const.SP, 0x7);
    }
    #endregion

    #region 机器码映射
    private void AddDic()
    {
        opcodeDic.Add(0x00, new NOP());
        opcodeDic.Add(0x80, new SJMP());

        AddBitAlgorithm();
        AddAlgorithm();
        AddLogic();
        AddXCH();
        ADDMove();
        AddPop_Push();
        AddJMP();
    }
    private void AddBitAlgorithm()
    {
        opcodeDic.Add(0x82,new ANL_C_BIT());
        opcodeDic.Add(0xB0,new ANL_C_NOT_BIT());
        opcodeDic.Add(0x72, new ORL_C_BIT());
        opcodeDic.Add(0xA0, new ORL_C_NOT_BIT());
        opcodeDic.Add(0xC3,new CLR_C());
        opcodeDic.Add(0xC2, new CLR_BIT());
        opcodeDic.Add(0xD3,new SETB_C());
        opcodeDic.Add(0xD2,new SETB_BIT());
        opcodeDic.Add(0xB3,new CPL_C());//取反
        opcodeDic.Add(0xB2,new CPL_BIT());
        opcodeDic.Add(0x40,new JC_rel());
        opcodeDic.Add(0x50,new JNC_rel());

        opcodeDic.Add(0x10, new JBC_BIT_rel());
        opcodeDic.Add(0x20,new JB_BIT_rel());
        opcodeDic.Add(0x30,new JNB_BIT_rel());

    }
    private void AddJMP()
    {
        //AJMP addr11
        opcodeDic.Add(0x01,new AJMP_ADDR());
        opcodeDic.Add(0x21,new AJMP_ADDR());
        opcodeDic.Add(0x41,new AJMP_ADDR());
        opcodeDic.Add(0x61,new AJMP_ADDR());
        opcodeDic.Add(0x81,new AJMP_ADDR());
        opcodeDic.Add(0xA1,new AJMP_ADDR());
        opcodeDic.Add(0xC1,new AJMP_ADDR());
        opcodeDic.Add(0xE1,new AJMP_ADDR());
        //LJMP addr16
        opcodeDic.Add(0x02, new LJMP_ADDR());
        //ACALL addr11
        opcodeDic.Add(0x11, new ACALL_ADDR());
        opcodeDic.Add(0x31, new ACALL_ADDR());
        opcodeDic.Add(0x51, new ACALL_ADDR());
        opcodeDic.Add(0x71, new ACALL_ADDR());
        opcodeDic.Add(0x91, new ACALL_ADDR());
        opcodeDic.Add(0xB1, new ACALL_ADDR());
        opcodeDic.Add(0xD1, new ACALL_ADDR());
        opcodeDic.Add(0xF1, new ACALL_ADDR());
        //LCALL addr16
        opcodeDic.Add(0x12, new LCALL_ADDR());
        //RET
        opcodeDic.Add(0x22,new RET());
        //RETI
        opcodeDic.Add(0x32,new RETI());
        //JMP @A+DPTR
        opcodeDic.Add(0x73, new JMP_AT_ACC_DPTR());
        //条件跳转
        opcodeDic.Add(0x60, new JZ_rel());
        opcodeDic.Add(0x70, new JNZ_rel());
        opcodeDic.Add(0xD8, new DJNZ_Rn_rel());
        opcodeDic.Add(0xD9, new DJNZ_Rn_rel());
        opcodeDic.Add(0xDA,new DJNZ_Rn_rel());
        opcodeDic.Add(0xDB,new DJNZ_Rn_rel());
        opcodeDic.Add(0xDC,new DJNZ_Rn_rel());
        opcodeDic.Add(0xDD,new DJNZ_Rn_rel());
        opcodeDic.Add(0xDE, new DJNZ_Rn_rel());
        opcodeDic.Add(0xDF, new DJNZ_Rn_rel());
        opcodeDic.Add(0xD5, new DJNZ_DIRECT_rel());

        opcodeDic.Add(0xB5, new CJNE_ACC_DIRECT_rel());
        opcodeDic.Add(0xB4, new CJNE_ACC_IMM_rel());
        opcodeDic.Add(0xB8, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xB9, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xBA, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xBB, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xBC, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xBD, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xBE, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xBF, new CJNE_Rn_IMM_rel());
        opcodeDic.Add(0xB6, new CJNE_AT_Ri_IMM_rel());
        opcodeDic.Add(0xB7, new CJNE_AT_Ri_IMM_rel());
    }
    private void AddAlgorithm()
    {
        //ADD
        opcodeDic.Add(0x24, new ADD_A_IMM());
        opcodeDic.Add(0x25, new ADD_A_DIRECT());
        opcodeDic.Add(0x26, new ADD_A_AT_Ri());
        opcodeDic.Add(0x27, new ADD_A_AT_Ri());
        opcodeDic.Add(0x28, new ADD_A_Rn());
        opcodeDic.Add(0x29, new ADD_A_Rn());
        opcodeDic.Add(0x2A, new ADD_A_Rn());
        opcodeDic.Add(0x2B, new ADD_A_Rn());
        opcodeDic.Add(0x2C, new ADD_A_Rn());
        opcodeDic.Add(0x2D, new ADD_A_Rn());
        opcodeDic.Add(0x2E, new ADD_A_Rn());
        opcodeDic.Add(0x2F, new ADD_A_Rn());
        //ADDC
        opcodeDic.Add(0x34, new ADDC_A_IMM());
        opcodeDic.Add(0x35, new ADDC_A_DIRECT());
        opcodeDic.Add(0x36, new ADDC_A_AT_Ri());
        opcodeDic.Add(0x37, new ADDC_A_AT_Ri());
        opcodeDic.Add(0x38, new ADDC_A_Rn());
        opcodeDic.Add(0x39, new ADDC_A_Rn());
        opcodeDic.Add(0x3A, new ADDC_A_Rn());
        opcodeDic.Add(0x3B, new ADDC_A_Rn());
        opcodeDic.Add(0x3C,new ADDC_A_Rn());
        opcodeDic.Add(0x3D,new  ADDC_A_Rn());
        opcodeDic.Add(0x3E,new  ADDC_A_Rn());
        opcodeDic.Add(0x3F, new ADDC_A_Rn());
        //INC
        opcodeDic.Add(0x04, new INC_A());
        opcodeDic.Add(0x05, new INC_DIRECT());
        opcodeDic.Add(0x06, new INC_AT_Ri());
        opcodeDic.Add(0x07, new INC_AT_Ri());
        opcodeDic.Add(0x08, new INC_Rn());
        opcodeDic.Add(0x09, new INC_Rn());
        opcodeDic.Add(0x0A, new INC_Rn());
        opcodeDic.Add(0x0B, new INC_Rn());
        opcodeDic.Add(0x0C, new INC_Rn());
        opcodeDic.Add(0x0D, new INC_Rn());
        opcodeDic.Add(0x0E, new INC_Rn());
        opcodeDic.Add(0x0F, new INC_Rn());
        opcodeDic.Add(0xA3, new INC_DPTR());
        //DEC
        opcodeDic.Add(0x14, new DEC_A());
        opcodeDic.Add(0x15, new DEC_DIRECT());
        opcodeDic.Add(0x16, new DEC_AT_Ri());
        opcodeDic.Add(0x17, new DEC_AT_Ri());
        opcodeDic.Add(0x18, new DEC_Rn());
        opcodeDic.Add(0x19, new DEC_Rn());
        opcodeDic.Add(0x1A, new DEC_Rn());
        opcodeDic.Add(0x1B, new DEC_Rn());
        opcodeDic.Add(0x1C, new DEC_Rn());
        opcodeDic.Add(0x1D, new DEC_Rn());
        opcodeDic.Add(0x1E, new DEC_Rn());
        opcodeDic.Add(0x1F, new DEC_Rn());
        //SUBB
        opcodeDic.Add(0x94, new SUBB_A_IMM());
        opcodeDic.Add(0x95, new SUBB_A_DIRECT());
        opcodeDic.Add(0x96, new SUBB_A_AT_Ri());
        opcodeDic.Add(0x97, new SUBB_A_AT_Ri());
        opcodeDic.Add(0x98, new SUBB_A_Rn());
        opcodeDic.Add(0x99, new SUBB_A_Rn());
        opcodeDic.Add(0x9A, new SUBB_A_Rn());
        opcodeDic.Add(0x9B, new SUBB_A_Rn());
        opcodeDic.Add(0x9C, new SUBB_A_Rn());
        opcodeDic.Add(0x9D, new SUBB_A_Rn());
        opcodeDic.Add(0x9E, new SUBB_A_Rn());
        opcodeDic.Add(0x9F, new SUBB_A_Rn());
        //MUL
        opcodeDic.Add(0xA4, new MUL_AB());
        opcodeDic.Add(0x84, new DIV_AB());
        opcodeDic.Add(0xD4, new DA_A());
    }
    private void AddLogic()
    {
        opcodeDic.Add(0x58, new ANL_ACC_Rn());
        opcodeDic.Add(0x59, new ANL_ACC_Rn());
        opcodeDic.Add(0x5A, new ANL_ACC_Rn());
        opcodeDic.Add(0x5B, new ANL_ACC_Rn());
        opcodeDic.Add(0x5C, new ANL_ACC_Rn());
        opcodeDic.Add(0x5D, new ANL_ACC_Rn());
        opcodeDic.Add(0x5E, new ANL_ACC_Rn());
        opcodeDic.Add(0x5F, new ANL_ACC_Rn());
        opcodeDic.Add(0x55, new ANL_ACC_DIRECT());
        opcodeDic.Add(0x56, new ANL_ACC_AT_Ri());
        opcodeDic.Add(0x57, new ANL_ACC_AT_Ri());
        opcodeDic.Add(0x54, new ANL_ACC_IMM());
        opcodeDic.Add(0x52, new ANL_DIRECT_ACC());
        opcodeDic.Add(0x53, new ANL_DIRECT_IMM());

        opcodeDic.Add(0x48, new ORL_ACC_Rn());
        opcodeDic.Add(0x49, new ORL_ACC_Rn());
        opcodeDic.Add(0x4A, new ORL_ACC_Rn());
        opcodeDic.Add(0x4B, new ORL_ACC_Rn());
        opcodeDic.Add(0x4C, new ORL_ACC_Rn());
        opcodeDic.Add(0x4D, new ORL_ACC_Rn());
        opcodeDic.Add(0x4E, new ORL_ACC_Rn());
        opcodeDic.Add(0x4F, new ORL_ACC_Rn());
        opcodeDic.Add(0x45, new ORL_ACC_DIRECT());
        opcodeDic.Add(0x46, new ORL_ACC_AT_Ri());
        opcodeDic.Add(0x47, new ORL_ACC_AT_Ri());
        opcodeDic.Add(0x44, new ORL_ACC_IMM());
        opcodeDic.Add(0x42, new ORL_DIRECT_ACC());
        opcodeDic.Add(0x43, new ORL_DIRECT_IMM());

        opcodeDic.Add(0x68, new XRL_ACC_Rn());
        opcodeDic.Add(0x69, new XRL_ACC_Rn());
        opcodeDic.Add(0x6A, new XRL_ACC_Rn());
        opcodeDic.Add(0x6B, new XRL_ACC_Rn());
        opcodeDic.Add(0x6C, new XRL_ACC_Rn());
        opcodeDic.Add(0x6D, new XRL_ACC_Rn());
        opcodeDic.Add(0x6E, new XRL_ACC_Rn());
        opcodeDic.Add(0x6F, new XRL_ACC_Rn());
        opcodeDic.Add(0x65, new XRL_ACC_DIRECT());
        opcodeDic.Add(0x66, new XRL_ACC_AT_Ri());
        opcodeDic.Add(0x67, new XRL_ACC_AT_Ri());
        opcodeDic.Add(0x64, new XRL_ACC_IMM());
        opcodeDic.Add(0x62, new XRL_DIRECT_ACC());
        opcodeDic.Add(0x63, new XRL_DIRECT_IMM());

        opcodeDic.Add(0xE4, new CLR_A());
        opcodeDic.Add(0xF4, new CPL_A());

        opcodeDic.Add(0x23, new RL_A());
        opcodeDic.Add(0x33, new RLC_A());

        opcodeDic.Add(0x03, new RR_A());
        opcodeDic.Add(0x13, new RRC_A());


    }
    private void AddXCH()
    {
        opcodeDic.Add(0xC4, new SWAP_A());
        opcodeDic.Add(0xC5, new XCH_A_DIRECT());
        opcodeDic.Add(0xC6, new XCH_A_AT_Ri());
        opcodeDic.Add(0xC7, new XCH_A_AT_Ri());
        opcodeDic.Add(0xC8, new XCH_A_Rn());
        opcodeDic.Add(0xC9, new XCH_A_Rn());
        opcodeDic.Add(0xCA, new XCH_A_Rn());
        opcodeDic.Add(0xCB, new XCH_A_Rn());
        opcodeDic.Add(0xCC, new XCH_A_Rn());
        opcodeDic.Add(0xCD, new XCH_A_Rn());
        opcodeDic.Add(0xCE, new XCH_A_Rn());
        opcodeDic.Add(0xCF, new XCH_A_Rn());
        opcodeDic.Add(0xD6, new XCHD_A_AT_Ri());
        opcodeDic.Add(0xD7, new XCHD_A_AT_Ri());
    }
    private void AddPop_Push()
    {
        opcodeDic.Add(0xC0, new PUSH_DIRECT());
        opcodeDic.Add(0xD0, new POP_DIRECT());
    }
    private void ADDMove()
    {
        opcodeDic.Add(0x74, new MOVE_ACC_IMM());
        opcodeDic.Add(0x75, new MOVE_DIRECT_IMM());
        opcodeDic.Add(0x76, new MOVE_At_Ri_IMM());
        opcodeDic.Add(0x77, new MOVE_At_Ri_IMM());
        opcodeDic.Add(0x78, new MOVE_Rn_IMM());
        opcodeDic.Add(0x79, new MOVE_Rn_IMM());
        opcodeDic.Add(0x7A, new MOVE_Rn_IMM());
        opcodeDic.Add(0x7B, new MOVE_Rn_IMM());
        opcodeDic.Add(0x7C, new MOVE_Rn_IMM());
        opcodeDic.Add(0x7D, new MOVE_Rn_IMM());
        opcodeDic.Add(0x7E, new MOVE_Rn_IMM());
        opcodeDic.Add(0x7F, new MOVE_Rn_IMM());
        opcodeDic.Add(0x83, new MOVC_ACC_At_ACC_PC());
        opcodeDic.Add(0x85, new MOVE_DIRECT_DIRECT());//这个指令 des 和 src是颠倒的
        opcodeDic.Add(0x86, new MOVE_DIRECT_At_Rn());
        opcodeDic.Add(0x87, new MOVE_DIRECT_At_Rn());
        opcodeDic.Add(0x88, new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x89, new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x8A,new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x8B,new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x8C,new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x8D,new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x8E,new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x8F, new MOVE_DIRECT_Rn());
        opcodeDic.Add(0x90, new MOVE_DPTR_IMM16());
        opcodeDic.Add(0x92, new MOVE_BIT_Cy());
        opcodeDic.Add(0x93, new MOVC_ACC_At_ACC_DPTR());
        opcodeDic.Add(0xA2,new MOVE_Cy_BIT());
        opcodeDic.Add(0xA6,new MOVE_At_Ri_DIRECT());
        opcodeDic.Add(0xA7,new MOVE_At_Ri_DIRECT());
        opcodeDic.Add(0xA8,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xA9,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xAA,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xAB,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xAC,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xAD,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xAE,new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xAF, new MOVE_Rn_DIRECT());
        opcodeDic.Add(0xE0,new MOVX_ACC_AT_DPTR());
        opcodeDic.Add(0xE2,new MOVX_ACC_AT_Ri());
        opcodeDic.Add(0xE3,new MOVX_ACC_AT_Ri());
        opcodeDic.Add(0xE5,new MOVE_ACC_DIRECT());
        opcodeDic.Add(0xE6,new MOVE_ACC_At_Ri());//间接寻址 R0,R1
        opcodeDic.Add(0xE7,new MOVE_ACC_At_Ri());
        opcodeDic.Add(0xE8,new MOVE_ACC_Rn());
        opcodeDic.Add(0xE9,new MOVE_ACC_Rn());
        opcodeDic.Add(0xEA, new MOVE_ACC_Rn());
        opcodeDic.Add(0xEB, new MOVE_ACC_Rn());
        opcodeDic.Add(0xEC, new MOVE_ACC_Rn());
        opcodeDic.Add(0xED, new MOVE_ACC_Rn());
        opcodeDic.Add(0xEE, new MOVE_ACC_Rn());
        opcodeDic.Add(0xEF, new MOVE_ACC_Rn());

        opcodeDic.Add(0xF0, new MOVX_AT_DPTR_ACC());
        opcodeDic.Add(0xF2, new MOVX_AT_Ri_ACC());
        opcodeDic.Add(0xF3, new MOVX_AT_Ri_ACC());

        opcodeDic.Add(0xF5, new MOVE_DIRECT_ACC());
        opcodeDic.Add(0xF6, new MOVE_At_Ri_ACC());
        opcodeDic.Add(0xF7, new MOVE_At_Ri_ACC());
        opcodeDic.Add(0xF8, new MOVE_Rn_ACC());
        opcodeDic.Add(0xF9, new MOVE_Rn_ACC());
        opcodeDic.Add(0xFA, new MOVE_Rn_ACC());
        opcodeDic.Add(0xFB, new MOVE_Rn_ACC());
        opcodeDic.Add(0xFC, new MOVE_Rn_ACC());
        opcodeDic.Add(0xFD, new MOVE_Rn_ACC());
        opcodeDic.Add(0xFE, new MOVE_Rn_ACC());
        opcodeDic.Add(0xFF, new MOVE_Rn_ACC());
    }
    #endregion

    #region 内存 
    public bool isRunning = false;
    public bool in_interupt = false;
    private Dictionary<byte, InstructInfo> opcodeDic;
    public _instruct instr;
    private VM_InteruptSystem vm_Interupt;
    private VM_Timer vM_Timer;
    public ushort PC;
    public int cycles;
    private byte[] rom;
    private byte[] sfr_ram;
    private byte[] external_ram;
    #endregion

    #region  加载、执行处理
    public void LoadProgram(byte[] program)
    {
        rom = program;
    }
    private void ExecuteInstruction()
    {
        //取指令
        instr.opcode = rom[PC];
        if (!opcodeDic.ContainsKey(instr.opcode)) { Debug.Log("没有该操作码"); return; }
        //根据PC指针，从ROM中取指令,赋值指令信息类，赋值操作数
        instr.opcode = rom[PC];
        InstructInfo info = opcodeDic[instr.opcode];
        instr.info = info;
        instr.op0 = (info.bytes > 1) ? rom[PC + 1] : (byte)0;
        instr.op1 = (info.bytes > 2) ? rom[PC + 2] : (byte)0;
        //执行
        instr.info.exec(instr);
    }
    public void Run()
    {
        isRunning = true;
        do
        {
            //输入检测
            InputCheck();
            //指令执行
            ExecuteInstruction();
            //PSW检验
            Update_PSW_P();
            //定时器
            vM_Timer.Update_timer(instr.info.cycles);
            //中断检测
            vm_Interupt.InteruptDetect();
            //输出检测
            OutputCheck();
        } while (isRunning);
    }
    private void Update_PSW_P()
    {
        byte a = Read_Sfr(Const.A);
        int count = 0;
        bool handler = true;
        while (handler)
        {
            if ((a & 0x1) != 0x0)
            {
                count++;
            }
            a >>= 1;
            if (a == 0x0)
            {
                handler = false;
            }
        }
        Write_Bit(Const.PSW_P, count & 0x1);//奇数第一位一定是1,PSW_P 置为1
    }
    #endregion

    #region 读写内存 寻址 程序等

    public void Write_Sfr(byte addr, byte data)
    {
        if (addr >= Const.sfr_addr_start) sfr_ram[addr] = data;
    }
    public byte Read_Sfr(byte addr)
    {
        byte data = (addr >= Const.sfr_addr_start) ? sfr_ram[addr] : (byte)0;
        byte inputValue = GetInputCheckData(addr);
        return (byte)(data & inputValue);
    }
    public void Write_Bit(byte addr, int bit)
    {
        Write_Bit_Default(addr, bit);
    }

    //位寻址 是 0x20~0x2f编得的128个位的地址，下面的addr是与其他不同的
    public byte Read_Bit(byte addr)
    {
        byte data = Read_Bit_Default(addr);
        
        return data;
    }
    private int get_psw_rs()
    {
        return (Read_Sfr(Const.PSW)>>3)&0x3;
    }
    public ushort Read_Op(_instruct instruct, int opPosition, mem_type type)
    {
        //读取操作数的数据(可能时在寄存器中，可能是立即数，可能是直接寻址)
        InstructInfo info = instruct.info;
        //找到要读取的op的类型，(立即数，直接地址，寄存器地址，。。。) opPosition=1,就读1
        OpType op_mode = opPosition > 0 ? info.op1_mode : info.op0_mode;
        byte op = getOp(instruct, opPosition);
        switch (op_mode)
        {
            case OpType.NONE:
                return 0;
            case OpType.Rn:
                {
                    //0000 0111 = 0x07
                    int reg = instruct.opcode & 0x07;
                    return sfr_ram[reg+get_psw_rs()*8];
                }
            case OpType.IMM8:
                return op;
            case OpType.ACC:
                return Read_Sfr(Const.A);
            case OpType.DIRECT:
                return Read_Ram(op);
            case OpType.DPTR:
                return (ushort)((Read_Sfr(Const.DPTR_H) << 8) + Read_Sfr(Const.DPTR_L));
            case OpType.IMM16:
                return (ushort)((instruct.op0 << 8) | instruct.op1);
            case OpType.R0_R1:
                byte addr = sfr_ram[instruct.opcode & 0x01+get_psw_rs() * 8];//找到Ri中存的地址，再寻址其中的数据
                return Read_Ram(addr);
            case OpType.PSW_Cy:
                return Read_Bit(Const.PSW_CY);
            case OpType.BIT:
                return Read_Bit(op);
            case OpType.ACC_DPTR:
                ushort dptr = (ushort)((Read_Sfr(Const.DPTR_H) << 8) | Read_Sfr(Const.DPTR_L));
                ushort addr_A_D = (ushort)(Read_Sfr(Const.A) + dptr);
                return VmRead(type, addr_A_D);
            case OpType.ACC_PC://对PC有影响，base.exec(instr)  要放之后
                ushort addr_A_P = (ushort)(Read_Sfr(Const.A) + (PC + 1));
                return VmRead(type, addr_A_P);
            case OpType.AT_DPTR:
                ushort addr_AT_D = (ushort)((Read_Sfr(Const.DPTR_H) << 8) | Read_Sfr(Const.DPTR_L));
                return VmRead(type, addr_AT_D);
            case OpType.AT_SP:
                ushort addr_dir = Read_Sfr(Const.SP);
                return VmRead(type, (byte)addr_dir);
            case OpType.B:
                return Read_Sfr(Const.B);
            default:
                return 0;
        }
    }
    public void Write_Op(_instruct instruct, ushort data, int opPosition, mem_type type)
    {
        InstructInfo info = instruct.info;
        OpType op_mode = opPosition > 0 ? info.op1_mode : info.op0_mode;//操作数寻址模式
        byte op = getOp(instruct, opPosition);
        //IMM8 IMM16 不会写立即数
        switch (op_mode)
        {
            case OpType.NONE:
                break;
            case OpType.Rn:
                int reg = instruct.opcode & 0x07;
                sfr_ram[reg + get_psw_rs() * 8] = (byte)data;
                break;
            case OpType.ACC:
                Write_Sfr(Const.A, (byte)data);
                break;
            case OpType.DIRECT:
                Write_Ram(op, (byte)data);
                break;
            case OpType.DPTR:
                Write_Sfr(Const.DPTR_H, (byte)(data >> 8));
                Write_Sfr(Const.DPTR_L, (byte)data);
                break;
            case OpType.R0_R1:
                byte addr = sfr_ram[instruct.opcode & 0x1+get_psw_rs() * 8];
                Write_Ram(addr, (byte)data);
                break;
            case OpType.PSW_Cy:
                Write_Bit(Const.PSW_CY, data);
                break;
            case OpType.BIT:
                Write_Bit(op, (byte)data);
                break;
            case OpType.AT_DPTR:
                ushort dptr = (ushort)((Read_Sfr(Const.DPTR_H) << 8) + Read_Sfr(Const.DPTR_L));
                VmWrite(type, dptr, (byte)data);
                break;
            case OpType.AT_SP:
                ushort addr_dir = Read_Sfr(Const.SP);
                VmWrite(type, (byte)addr_dir, (byte)data);
                break;
            case OpType.B:
                Write_Sfr(Const.B, (byte)data);
                break;
            default:
                break;
        }
    }
    public byte VmRead(mem_type type, ushort addr)
    {
        switch (type)
        {
            case mem_type.CODE:
                return Read_Code(addr);
            case mem_type.RAM:
                return Read_Ram((byte)addr);
            case mem_type.SFR:
                return Read_Sfr((byte)addr);
            case mem_type.BIT:
                return Read_Bit((byte)addr);
            case mem_type.Exteranl:
                return Read_External(addr);
            default:
                return 0;
        }
    }
    public void VmWrite(mem_type type, ushort addr, byte data)
    {
        switch (type)
        {
            case mem_type.CODE:
                Write_Code(addr, data);
                break;
            case mem_type.RAM:
                Write_Ram((byte)addr, data);
                break;
            case mem_type.SFR:
                Write_Sfr((byte)addr, data);
                break;
            case mem_type.BIT:
                Write_Bit((byte)addr, data);
                break;
            case mem_type.Exteranl:
                Write_External(addr, data);
                break;
            default:
                break;
        }
    }

    //从指令中获得相应类型的字节 不是所有类型都需要从这里获取数据
    private byte getOp(_instruct instruct, int opPosition)
    {
        InstructInfo info = instruct.info;
        if (opPosition == 0)
        {
            switch (info.op0_mode)
            {
                case OpType.IMM8:
                case OpType.DIRECT:
                case OpType.BIT:
                    return instruct.op0;//这个也可以是 RAM中的地址(直接地址)
                default:
                    return 0;
            }
        }
        else
        {
            return info.bytes > 2 ? instruct.op1 : instruct.op0;
        }
    }
    public byte Read_Ram(byte addr)
    {
        byte data;
        if (addr >= 128)
            data = Read_Sfr(addr);
        else
            data = sfr_ram[addr];
        return data;
    }
    public void Write_Ram(byte addr, byte data)
    {
        if (addr >= 128)
        {
            Write_Sfr(addr, data);
        }
        else
        {
            sfr_ram[addr] = data;
        }
    }
    private byte Read_Code(ushort addr)
    {
        return rom[addr];
    }
    private void Write_Code(ushort addr, byte data)
    {
        rom[addr] = data;
    }
    private byte Read_External(ushort addr)
    {
        return external_ram[addr];
    }
    private void Write_External(ushort addr, byte data)
    {
        external_ram[addr] = data;
    }
    private void Write_Bit_Default(byte addr, int bit)
    {
        byte bit_offset = (byte)(addr % 8);
        bit &= 0x1;
        byte byte_idx = addr < Const.sfr_addr_start? (byte)(addr / 8 + Const.ram_bit_addr_start): (byte)(addr / 8 * 8);
        sfr_ram[byte_idx] &= (byte)~(1 << bit_offset);
        sfr_ram[byte_idx] |= (bit | 0x0) != 0x0 ? (byte)(1 << bit_offset) : (byte)0;
    }
    private byte Read_Bit_Default(byte addr)
    {
        byte bit_offset = (byte)(addr % 8);//得到偏移量
        byte byte_idx = addr < Const.sfr_addr_start? (byte)(addr / 8 + Const.ram_bit_addr_start): (byte)(addr / 8 * 8);
        byte data = GetInputCheckData(byte_idx);
        return (sfr_ram[byte_idx]& data & (1 << bit_offset)) != 0 ? (byte)1 : (byte)0;
    }
    #endregion
}