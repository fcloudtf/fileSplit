
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSplit
{
    /// <summary>
    /// 存储空间单位
    /// </summary>
    public enum units { B, KB, MB, GB, TB, PB, EB, ZB, YB, BB, NB, DB };

    /// <summary>
    /// 此类用于实现存储单位转化，
    /// 文件大小3种表示方式： 1.字节大小 2.字符串 3.数值+单位
    /// </summary>
    public class FileLen
    {
        /// <summary>
        /// 文件字节大小
        /// </summary> 
        public long Len = 0;
        /// <summary>
        /// 文件大小，字符串形式
        /// </summary>
        public string Str = "0 B";
        /// <summary>
        /// 与存储单位对应的大小
        /// </summary>
        public float Num = 0;
        /// <summary>
        /// 存储单位
        /// </summary>
        public units Ext = units.B;

        /// <summary>
        /// 从字节大小构建
        /// </summary>
        public FileLen(long len)
        {
            Len = len;

            int i = 0;
            Num = len;
            while (len >= 1024 && i <= (int)(units.DB))
            {
                Num = len / 1024f;
                len = (int)Num;
                i++;
            }
            Ext = (units)i;

            Str = Num.ToString("F2") + " " + Ext.ToString();
        }

        /// <summary>
        /// 从字符串构建, str形如"32.14 MB"
        /// </summary>
        public FileLen(string str)
        {
            string[] tmp = str.Split(' ');
            if (tmp.Length != 2) return; 

            Str = str;
            Num = float.Parse(tmp[0]);
            Ext = (units)Enum.Parse(typeof(units), tmp[1]);

            Len = (long)Num;
            int i = (int)Ext;
            while (i>0)
            {
                Len *= 1024;
                i--;
            }
        }

        /// <summary>
        /// 从给定值和容量单位创建， num = 123.23f ext = "KB"
        /// </summary>
        public FileLen(float num, string ext)
        {
            Num = num;
            Ext = (units)Enum.Parse(typeof(units), ext);
            Str = num.ToString() + " " + ext.ToString();

            Len = (long)Num;
            int i = (int)Ext;
            while (i > 0)
            {
                if ((long)(Len * 1024) <= 0) //long越界,保留最大值
                    Len = long.MaxValue;   
                else Len *= 1024;
                i--;
            }
        }

    }
}
