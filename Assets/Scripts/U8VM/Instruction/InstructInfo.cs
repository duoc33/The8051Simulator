

/// <summary>
//�� ����Ѱַ����������ָ���ֽڵ���ʽ���ڳ���洢���У�Ϊ8λ��16λ������:MOV A, #30H��MOV DPTR, #8000H
//�� �Ĵ���Ѱַ������������ѡ���ļĴ����У���R0-R7��A��B��DPTR������: MOV A, R3
//�� ֱ��Ѱַ����ָ���и����˲�������ʵ�ʵ�ַ�����磺MOV A, 30H��ANL 40H, #33H
//�� �Ĵ������Ѱַ���������ĵ�ַ����ѡ���ļĴ����У���@R0-R7��SP��@DPTR������: MOV @R0, A
//�� ��ַ+��ַ���Ѱַ����DPTR��PC����ַ�Ĵ�����A����ַ�Ĵ���������γ�16λ�ĵ�ַ�����ʳ���洢��������:MOVC A, @A+DPTR
//�� ���Ѱַ���Գ��������PC������Ϊ����ַ������ָ���е�ƫ���������ý��Ϊ��ת��Ŀ���ַ������: JNZ rel
/// </summary>
/// 
    //ָ����Ϣ��
    public class InstructInfo
    {
        //�ֽ���
        public ushort bytes;
        //ָ������
        public int cycles;
        //����������
        public OpType op0_mode;
        public OpType op1_mode;
        //ָ����
        public string opcode_name;
        //ָ������߼�
        public virtual void exec(_instruct instr)
        {
            VM_8051_Mono.Instance.PC += instr.info.bytes;
            VM_8051_Mono.Instance.cycles += instr.info.cycles;
        }
    }
    //ָ��ṹ��
    public class _instruct
    {
        //������
        public byte opcode;
        //������0
        public byte op0;
        //������1
        public byte op1;
        //ָ����Ϣ�࣬����ָ���ֽ������������ںʹ����߼��ȡ�
        public InstructInfo info;
    }
    //���������ж�д������
    public enum OpType
    {
        NONE,
        Rn,//�Ĵ������� ������
        IMM8,//8λ������ ������
        ACC,
        DIRECT,
        DPTR,
        IMM16,
        R0_R1,
        PSW_Cy,
        BIT,
        ACC_DPTR, // ������ָ��ֻ�ᷢ���ڶ���ʱ�򣬲�����д�����
        ACC_PC,
        AT_DPTR,
        AT_SP,
        B
    }
    public enum mem_type
    {
        CODE,
        RAM,
        SFR,
        BIT,
        Exteranl,
    }


