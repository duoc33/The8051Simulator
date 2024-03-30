using System;
using System.IO;
using System.Linq;
    public class FileTool
    {
        #region Func2
        public static byte[] ExtractHexData(string filePath)
        {
            byte[] code = new byte[1024 * 64];
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    // ������ĩβ�Ļس��ͻ��з�
                    line = line.TrimEnd('\r', '\n');

                    // ������Ƿ��� ':' ��ʼ
                    if (line.StartsWith(":"))
                    {
                        // ����ÿ�е��ֽ�����
                        byte[] data = ParseHexRecord(line);
                        //���е����ݳ���(��������ʼ��ַ���������ͺͼ���) 16�����ֽ���
                        byte lineBytesCount = data[0];
                        //�������ݵ�PC��ַ
                        ushort addr = (ushort)(data[1] << 8 | data[2]);
                        //������������
                        byte type = data[3];
                        // ����е������Ƿ�Ϊ���ݼ�¼ (������Ϊ 00)
                        switch (type)
                        {
                            case 0x00:
                                {
                                    for (int i = 0; i < lineBytesCount; i++)
                                    {
                                        code[addr++] = data[i + 4];
                                    }
                                    break;
                                }
                            case 0x01:
                                {
                                    sr.Close();
                                    sr.Dispose();
                                    return code;
                                }
                            default:
                                UnityEngine.Debug.Log("�޸���������");
                                break;
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log("no : starts");
                        return null;
                    }
                }
            }
            // ���ؽ���ֽ�����
            return code;
        }
        private static byte[] ParseHexRecord(string line)
        {
            // ����ʮ�������ַ������������׵�ð��
            //����ָ����Χ�ڵ����������С� Enumerable.Range
            byte[] data = Enumerable.Range(0, line.Length / 2 - 1)
                                     .Select(i => Convert.ToByte(line.Substring(i * 2 + 1, 2), 16))
                                     .ToArray();
            return data;
        }
    #endregion

}
