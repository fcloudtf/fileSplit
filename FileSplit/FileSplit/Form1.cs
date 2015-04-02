
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Management;
using System.Runtime.InteropServices;

using System.IO;

namespace FileSplit
{
    public partial class Form1 : Form
    {

        Tool T = Tool.T();          //功能函数
        string[] OpendFilesName;    //待处理的文件
        long fileLen = 0;           //待处理文件的总大小

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //存储空间单位，初始化至下拉列表
            for (int i = 0; i <= (int)units.DB; i++)
            {
                units u = (units)(i);
                comboBox1.Items.Add(u.ToString()); 
            }

            //选中第一项
            comboBox1.SelectedIndex = 0;
        }

        //--------------------为Form添加文件拖拽处理逻辑----------------------------------------
        /// <summary>
        /// 文件或文件夹拖入
        /// </summary>
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            T.dragEnter(e);
        }

        /// <summary>
        /// drop时，获取拖入的文件名
        /// </summary>
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string filesName = T.dragDrop(e);      //拖入窗体的文件放下
            OpendFilesName = filesName.Split(';'); //分割为所有的文件名

            if (listBox1.Items.Count > 0)
            {
                listBox1.Items.Clear();     //清空列表
                fileLen = 0;                //清空累计大小
            }
            foreach(string file in OpendFilesName)
            {
                String name = System.IO.Path.GetFileName(file);     //获取文件名
                listBox1.Items.Add(name);                           //添加文件名到列表
                fileLen += new FileInfo(file).Length;               //累计文件大小
            }

            label3.Text = "文件总大小： " + new FileLen(fileLen).Str;//显示拖入的文件总大小

            //默认设置
            radioButton1.Checked = true;              
            radioButton_CheckedChanged(null, null);
            textBox1_TextChanged(null, null);
        }

        //--------------------文件分割与合并---------------------------------------------------
        /// <summary>
        /// 文件分割
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            //文件分割
            if (radioButton1.Checked)   //按指定份数进行分割
            {
                int subNum = (int)parse(textBox1.Text, 2);
                T.fileSplit(OpendFilesName, subNum, checkBox1.Checked, Int32.Parse(textBox3.Text.Trim()));
            }
            else
            {                           //按指定文件大小进行分割
                float num = parse(textBox2.Text, fileLen / 2);
                long size = new FileLen(num, comboBox1.SelectedItem.ToString()).Len;  
                T.fileSplit(OpendFilesName, size, checkBox1.Checked, Int32.Parse(textBox3.Text.Trim()));
            }

            //清空操作文件列表
            listBox1.Items.Clear();     
            label3.Text = "";
        }

        /// <summary>
        /// 文件合并
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            //文件合并
            T.fileCombine(GroupByName(OpendFilesName), checkBox1.Checked, -Int32.Parse(textBox3.Text.Trim()));

            //清空操作文件列表
            listBox1.Items.Clear();
            label3.Text = "";
        }

        //--------------------文件名分组排序---------------------------------------------------
        /// <summary>
        /// 将给定的子文件名，按前缀进行分组、计数
        /// 子文件名形如:"sci_android.rar@_1.split", 前缀sci_android
        /// </summary>
        public string[][] GroupByName(string[] names)
        {
            string[][] str = null;

            Dictionary<string, int> files = new Dictionary<string, int>();
            foreach (string name in names)
            {
                //若文件名中不含有"@_"或".split"，则可认定为不是当前工具导出的子文件   
                if (!name.Contains("@_") || !name.Contains(".split")) continue;  

                //获取子文件对应的原文件名 "sci_android.rar" + ".split"
                int i1 = name.LastIndexOf("@_"), i2 = name.LastIndexOf('.');
                string tmp = name.Remove(i1, i2 - i1);

                //统计对应的子文件数目
                if (files.ContainsKey(tmp)) files[tmp]++;
                else files.Add(tmp, 1);
            }

            //获取字典的所有键和值
            String[] keys = files.Keys.ToArray<string>();
            int[] values = files.Values.ToArray<int>();

            str = new string[keys.Length][];
            for (int i = 0; i < keys.Length; i++)
            {
                String key = keys[i];
                int value = values[i];

                int index = key.LastIndexOf('.');
                str[i] = new string[value];
                for (int j = 0; j < value; j++) 
                    str[i][j] = key.Substring(0, index) + "@_" + (j + 1) + key.Substring(index);
            }

            return str;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        //将字符串转化为浮点型数据，并在转化失败时提供一个默认值
        private float parse(string str, float defaultNum)
        {
            try { return float.Parse(str); }
            catch (Exception) { return defaultNum; }
        }

        /// <summary>
        /// 单选按钮选中状态变动
        /// </summary>
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = radioButton1.Checked;
            textBox2.Enabled = comboBox1.Enabled = radioButton2.Checked;
        }

        /// <summary>
        /// 设置密匙串
        /// </summary>
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.Visible = checkBox2.Checked;
        }

        /// <summary>
        /// 份数，值变动
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if(radioButton1.Checked)        //按份数分割，显示每份文件大小
            {
                FileLen tmp = new FileLen((long)(fileLen / parse(textBox1.Text, 1)));
                textBox2.Text = tmp.Num.ToString();
                comboBox1.SelectedIndex = (int)tmp.Ext;
            }
        }

        /// <summary>
        /// 大小，值变动
        /// </summary>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)  //按大小分割，显示文件份数
            {
                FileLen tmp = new FileLen(parse(textBox2.Text, fileLen), comboBox1.SelectedItem.ToString());
                textBox1.Text = (fileLen / tmp.Len + (fileLen % tmp.Len > 0 ? 1 : 0)).ToString();
            }
        }
    }
}
