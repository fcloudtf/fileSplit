
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.IO;

namespace FileSplit
{
    class Tool
    {
        private ProgressBar Progress;       //用于显示文件分割或合并的进度
        private long FileLen;               //待处理的文件总大小
        private long curLen;                //当前处理的文件大小
        /// <summary>
        /// 设置用于显示操作进度的进度条，和待处理文件的总大小
        /// </summary>
        public void setProgress(ProgressBar progress, long fileLen)
        {
            Progress = progress;
            Progress.Maximum = 1000;
            FileLen = fileLen;
            curLen = 0;
            progress.Value = 0;
        }

        static Tool Instance;
        /// <summary>
        /// 提供当前类的一个静态实例对象
        /// </summary>
        public static Tool T()
        {
            if (Instance == null) Instance = new Tool();
            return Instance;
        }


        //--------------------文件拖拽处理，获取所有文件名----------------------------- using System.Windows.Forms
        /// <summary>
        /// 文件拖进事件处理:
        /// 获取数据源的链接Link
        /// </summary>
        public void dragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))    //判断拖来的是否是文件
                e.Effect = DragDropEffects.Link;                //是则将拖动源中的数据连接到控件
            else e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// 文件放下事件处理：
        /// 放下, 另外需设置对应控件的 AllowDrop = true; 
        /// 获取的文件名形如 "d:\1.txt;d:\2.txt"
        /// </summary>
        public string dragDrop(DragEventArgs e)
        {
            string filesName = "";
            Array file = (System.Array)e.Data.GetData(DataFormats.FileDrop);//将拖来的数据转化为数组存储

            //判断是否为目录，从目录载入文件
            if (file.Length == 1)
            {
                string str = file.GetValue(0).ToString();
                if (!System.IO.File.Exists(str) && System.IO.Directory.Exists(str)) //拖入的不是文件，是文件夹
                {
                    string[] files = System.IO.Directory.GetFiles(str);
                    for (int i = 0; i < files.Length; i++)
                        filesName += (files[i] + (i == files.Length - 1 ? "" : ";"));

                    return filesName;
                }
            }

            //拖入的所有文件
            int len = file.Length;
            for (int i = 0; i < len; i++)
            {
                filesName += (file.GetValue(i).ToString() + (i == file.Length - 1 ? "" : ";"));
            }

            return filesName;
        }


        //--------------------文件，分割与合并---------------------------------------- using System.IO
        /// <summary>
        /// 单个文件分割函数,
        /// 可将任意文件fileIn分割为若干个子文件， 单个子文件最大为 len KB
        /// delet标识文件分割完成后是否删除原文件, change为加密密匙
        /// fileIn = "D:\\file.rar", 子文件名形如"D:\\file.rar@_1.split"
        /// </summary>
        public void fileSplit(String fileIn, long KBlen, bool delet, int change)
        {
            //输入文件校验
            if (fileIn == null || !System.IO.File.Exists(fileIn))
            {
                MessageBox.Show("文件" + fileIn + "不存在！");
                return;
            }

            //加密初始化
            short sign = 1;
            int num = 0, tmp;
            if (change < 0) { sign = -1; change = -change; }

            //从文件创建输入流
            FileStream FileIn = new FileStream(fileIn, FileMode.Open);
            byte[] data = new byte[KBlen < 1024 ? KBlen : 1024];   //流读取,缓存空间
            long len = 0, I = 1;            //记录子文件累积读取的KB大小, 分割的子文件序号

            FileStream FileOut = null;      //输出流
            int readLen = 0;                //每次实际读取的字节大小
            while (readLen > 0 || (readLen = FileIn.Read(data, 0, data.Length)) > 0) //读取数据
            {
                //创建分割后的子文件，已有则覆盖，子文件"D:\\1.rar@_1.split"
                if (len == 0) FileOut = new FileStream(fileIn + "@_" + I++ + ".split", FileMode.Create);
                len += readLen;              //累计读取的文件大小
                curLen += readLen;           //当前处理进度

                //加密逻辑,对data的首字节进行逻辑偏移加密
                if (num == 0) num = change;
                tmp = data[0] + sign * (num % 3 + 3);
                if (tmp > 255) tmp -= 255;
                else if (tmp < 0) tmp += 255;
                data[0] = (byte)tmp;
                num /= 3;

                //输出，缓存数据写入子文件
                FileOut.Write(data, 0, readLen);
                FileOut.Flush();

                //预读下一轮缓存数据
                readLen = FileIn.Read(data, 0, data.Length);
                if (len >= KBlen || readLen == 0)       //子文件达到指定大小，或文件已读完
                {
                    FileOut.Close();                    //关闭当前输出流
                    len = 0;
                }

                //显示处理进度
                if (Progress != null) Progress.Value = (int)(curLen / FileLen * Progress.Maximum);
            }

            FileIn.Close();                             //关闭输入流
            if (delet) System.IO.File.Delete(fileIn);   //删除源文件
        }

        /// <summary>
        /// 对多个文件进行分割操作
        /// </summary>
        public void fileSplit(String[] fileIn, long KBlen, bool delet, int change)
        {
            if (fileIn == null || fileIn.Length == 0) return;

            for (int i = 0; i < fileIn.Length; i++)
                fileSplit(fileIn[i], KBlen, delet, change);
            MessageBox.Show("文件分割完成！");
        }

        /// <summary>
        /// 单个文件分割函数,按指定的份数subNum进行分割
        /// </summary>
        public void fileSplit(String fileIn, int subNum, bool delet, int change)
        {
            long KBlen = new FileInfo(fileIn).Length;
            KBlen = KBlen / subNum + (KBlen % subNum > 0 ? 1 : 0);  //计算每份文件的大小

            fileSplit(fileIn, KBlen, delet, change);                //对文件进行分割
        }

        /// <summary>
        /// 对多个文件进行分割操作,按指定的份数subNum进行分割
        /// </summary>
        public void fileSplit(String[] fileIn, int subNum, bool delet, int change)
        {
            foreach(string file in fileIn) 
                fileSplit(file, subNum, delet, change);
            MessageBox.Show("文件分割完成！");
        }

        /// <summary>
        /// 文件合并函数,
        /// 可将任意个子文件合并为一个,为fileSplit()的逆过程
        /// delet标识是否删除原文件, change对data的首字节进行解密
        /// </summary>
        public void fileCombine(String[] fileIn, bool delet, int change)
        {
            //输入文件名校验
            if (fileIn == null || fileIn.Length == 0) return;

            //加密初始化
            short sign = 1;
            int num = 0, tmp;
            if (change < 0) { sign = -1; change = -change; }

            //从首个子文件解析原文件名, fileIn[0] = "D:\\1.rar@_1.split"
            
            string name = fileIn[0];
            int i1 = name.LastIndexOf("@_"), i2 = name.LastIndexOf('.');
            name = name.Substring(0, i2);                                   //剔除子文件拓展名".split"
            string fileOut = (i1 == -1) ? name : name.Remove(i1, i2 - i1);  //剔除"@_1" -> "D:\\1.rar.split"                      

            //创建输出文件，已有则覆盖
            FileStream FileOut = new FileStream(fileOut, FileMode.Create);

            for (int i = 0; i < fileIn.Length; i++)
            {
                //输入文件校验
                if (fileIn[i] == null || !System.IO.File.Exists(fileIn[i])) continue;

                //从子文件创建输入流
                FileStream FileIn = new FileStream(fileIn[i], FileMode.Open);
                byte[] data = new byte[1024];   //流读取,缓存空间
                int readLen = 0;                //每次实际读取的字节大小

                while ((readLen = FileIn.Read(data, 0, data.Length)) > 0)       //读取数据
                {
                    //解密逻辑,对data的首字节进行逻辑偏移解密
                    if (num == 0) num = change;
                    tmp = data[0] + sign * (num % 3 + 3);
                    if (tmp > 255) tmp -= 255;
                    else if (tmp < 0) tmp += 255;
                    data[0] = (byte)tmp;
                    num /= 3;

                    //输出，缓存数据写入文件
                    FileOut.Write(data, 0, readLen);
                    FileOut.Flush();

                    //显示处理进度
                    curLen += readLen;           //当前处理进度
                    if (Progress != null) Progress.Value = (int)(curLen / FileLen * Progress.Maximum);
                }

                //关闭输入流，删除源文件
                FileIn.Close();
                if (delet) System.IO.File.Delete(fileIn[i]);
            }

            FileOut.Close();    //关闭输出流
        }

        /// <summary>
        /// 对多组子文件进行合并
        /// </summary>
        public void fileCombine(String[][] fileIn, bool delet, int change)
        {
            for (int i = 0; i < fileIn.Length; i++)
                fileCombine(fileIn[i], delet, change);
            MessageBox.Show("文件合并完成！");
        }


        //--------------------文件，复制--------------------------------------------- using System.IO
        /// <summary>
        /// 复制文件fileIn到文件fileOut
        /// </summary>
        public void copyfile(String fileIn, String fileOut)
        {
            FileStream FileIn = new FileStream(fileIn, FileMode.Open);       //打开文件
            FileStream FileOut = new FileStream(fileOut, FileMode.Create);   //创建文件，有则覆盖

            byte[] tmp = new byte[1024];                //缓存空间
            int len = 0;
            while ((len = FileIn.Read(tmp, 0, tmp.Length)) > 0) //读取数据
            {
                FileOut.Write(tmp, 0, len);             //写到文件
                FileOut.Flush();
            }

            //关闭流
            FileIn.Close();
            FileOut.Close();
        }
    }
}

