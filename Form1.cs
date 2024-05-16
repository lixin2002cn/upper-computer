using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO.Ports;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.InteropServices;
using System.Threading;

namespace shangweiji
{
    public partial class Form1 : Form
    {   // 用于端口通信
        // 用于索引确定对应的时间
        // 测试时间，格式为"yyyy-MM-dd HH_mm_ss"
        public string Test_time;
        // 日志文件的保存名称
        public string file_name;
        // 日志文件的保存路径加上文件名
        public string save_path;
        // 返回给下位机的三个信息
        public int delay_time;
        public int connect_time;
        public int current;
        // 软件运行得到的保存路径
        public string select_path;
        public int baud;

        // 存储数据
        public int[] Signal1 = new int[120];
        public int[] Signal2 = new int[119];
        
        // 用于记录连续模式下进行了几组测试
        public int count;
        // 用于判断是否执行定时器中断代码，提示超时
        public bool flag_timer1;

      

        // 用于存储电压、电流的相位
        public int current_amptitude;
        public int current_phi;
        public int voltage_amptitude;
        public int voltage_phi;
        
        // 用来解决死锁问题，可以参考peterson算法
        /// <summary>
        /// 准备关闭串口=true
        /// </summary>
        private bool m_IsTryToClosePort = false;
        /// <summary>
        /// true表示正在接收数据
        /// </summary>
        private bool m_IsReceiving = false;

        public int singal_count = 0;

        //---定义  
        private delegate void ShowReceiveMessageCallBack(string text);
        //---声明一个委托  

        // 定义一个定时器
        System.Timers.Timer timerTimer;



        /*
        * 帧数据格式对照表：
        * 
        * 帧头    动作     帧长度    data[0]       data[1]       data[2]       data[3]       data[4]       data[5]     data[6] ....data[125]
        * Keep alive 信号(RECEIVE_SIGNAL)：
        * EF      81       02          60           06
        * Testing 信号(RECEIVE_SIGNAL):
        * EF      90       03      Hex_current     Hex_delay     Hex_connect
        * Response信号(RECEIVE_SIGNAL):
        * EF      91       7E      display_time1  display_time2    current_A   current_phi   volumn_A     volume_phi   signal1.....signal125   
        */

        public Form1()
        {
            // 初始化组件，这个语句是创建的时候文件自动生成的
            InitializeComponent();
            
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;

            // 初始化组件，这个是我们写的，这些主要是设置combobox的默认参数等
            component_init();



        }
        //下面三个语句是用来让程序等比例放大的

        [DllImport("user32.dll")]

        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0xB;


       
       

        

        private void Form1_Load(object sender, EventArgs e)

        {
            // 界面load的时候等比例放大组件
            this.Resize += new EventHandler(Form1_Resize);
            AutoSizeFormClass.X = this.Width;
            AutoSizeFormClass.Y = this.Height;
            AutoSizeFormClass.setTag(this);
            Form1_Resize(new object(), new EventArgs());
            // 添加一个数据接收事件
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);


        }
        // 这块也是等比例放大的代码，可以直接用，一些主要内容集成在了AutoSizeFormClass.cs中，不需要动，使用的时候将Form1.cs中的有关内容直接用就好
        private void Form1_Resize(object sender, EventArgs e) {
            SendMessage(this.Handle, WM_SETREDRAW, 0, IntPtr.Zero);
            float newx = (this.Width) / AutoSizeFormClass.X;
            float newy = this.Height / AutoSizeFormClass.Y;
            AutoSizeFormClass.setControls(newx, newy, this);
            SendMessage(this.Handle, WM_SETREDRAW, 1, IntPtr.Zero);
            this.Invalidate(true);
        }



        

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

 

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        

        // 按钮一是开始测试按钮
        private void button1_Click(object sender, EventArgs e)
        {



            // 初始化图表，按照两个函数的顺序来写
            chart_init();
            init_chart();
            // 初始化peterson算法的关闭进程的flag
            m_IsTryToClosePort = false;
            // 按下按钮以后 文本框应该保持没有数据
            textBox1.Text = null;
            
            flag_timer1 = false;
            // 单次测试情况
            if (radioButton1.Checked == true)
            {
                // 禁用多选按钮
                radioButton2.Enabled = false;
                // 单次测试的情况下没有间隔随时间
                comboBox7.Text = "无"; // 间隔时间为"无"
                // 参数未完全选择
                if (String.IsNullOrEmpty(comboBox1.Text) | String.IsNullOrEmpty(comboBox3.Text) | String.IsNullOrEmpty(comboBox4.Text) | String.IsNullOrEmpty(comboBox5.Text) | String.IsNullOrEmpty(comboBox6.Text) | String.IsNullOrEmpty(textBox3.Text))
                {

                    
                    // 弹出提醒
                    MessageBox.Show("请确保所有的参数已选择\r\n");
                   chart_init();
                }
                //参数完全选择，则可以进行单次测试处理
                else 
                {
                    // 初始化图表
                    chart_init();
                    //获取按钮按下的时间
                    Test_time = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH_mm_ss");
                    //获取历史文件保存的命名
                    file_name = Test_time + "_测试结果记录.txt";
                    /*try
                    {*/
                    // 串口初始化
                        port_init();
                    // 打开串口
                    try { serialPort1.Open();
                        label1.ForeColor = Color.Green;
                        // 如果连接正确，此时不可以重新修改参数设定
                        // 按钮一失能，即不能再使用开始测试按钮
                        button1.Enabled = false;
                        // 按钮二使能，即此时可以暂停测试
                        button2.Enabled = true;
                        // 一旦开始测试不能修改参数
                        comboBox1.Enabled = false;
                        comboBox3.Enabled = false;
                        comboBox4.Enabled = false;
                        comboBox5.Enabled = false;
                        comboBox6.Enabled = false;
                        comboBox7.Enabled = false;

                        // 指示灯的状态显示
                        textBox5.Text = "ON";
                        // 显示串口连接正常
                        // textBox2.AppendText("串口已连接\r\n"); // 显示串口已连接
                                                          //
                        /*
                         str_delay_time = comboBox5.Text;
                         str_connect_time = comboBox3.Text;
                         str_current = comboBox1.Text;
                        */
                        current = Value_table("current", comboBox1.Text);
                        connect_time = Value_table("connect_time", comboBox3.Text);
                        delay_time = Value_table("delay_time", comboBox5.Text);
                        baud = Value_table("Baud_ratio", comboBox4.Text);

                        // 发送测试信息帧
                        send_test_signal(current, delay_time, connect_time, baud);

                        // 定时器代码
                        build_timer();
                        //定时器开线程
                        timerTimer.Elapsed += Run;
                        // 定时器单次测试i，如果设置为true，那么每隔固定时间便会有超时提醒
                        timerTimer.AutoReset = false;
                        timerTimer.Enabled = true;
                    }
                    catch
                    {
                        MessageBox.Show("当前串口被占用或不存在，请重新选择");
                        if (serialPort1.IsOpen)
                            DisconnectDeveice();

                    }                   // 打开串口
                                       
                    
                } 

            }
            // 连续模式
            else
            {
                radioButton1.Enabled = false;
                if (comboBox7.Text == "无")
                { 
                    // 弹窗提醒连续模式需选择连续事件
                    MessageBox.Show("连续模式下必须有间隔时间");
                }
                else {
                    if (String.IsNullOrEmpty(comboBox1.Text) | String.IsNullOrEmpty(comboBox3.Text) | String.IsNullOrEmpty(comboBox4.Text) | String.IsNullOrEmpty(comboBox5.Text) | String.IsNullOrEmpty(comboBox6.Text) | String.IsNullOrEmpty(textBox3.Text))
                    {
                        textBox2.Text = null;
                        MessageBox.Show("请确保所有的参数已选择\r\n");
                    }
                    //参数完全选择，则可以进行测试处理
                    else
                    {
                        // 初始化图表
                        chart_init();
                        count = 0;
                        //获取按钮按下的时间
                        Test_time = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH_mm_ss");
                        //获取历史文件保存的命名
                        file_name = Test_time + "_测试结果记录.txt";
                        /*try
                        {*/
                        // 串口初始化
                        port_init();
                        // 打开串口
                        try { serialPort1.Open();
                            Thread.Sleep(100);
                            // 打开串口
                            //如果正常运行，这时候指示灯会显示绿色
                            label1.ForeColor = Color.Green;
                            // 如果连接正确，此时不可以重新修改参数设定
                            // 按钮一失能，即不能再使用开始测试按钮
                            button1.Enabled = false;
                            // 按钮二使能，即此时可以暂停测试
                            button2.Enabled = true;
                            // 一旦开始测试不能修改参数
                            comboBox1.Enabled = false;
                            comboBox3.Enabled = false;
                            comboBox4.Enabled = false;
                            comboBox5.Enabled = false;
                            comboBox6.Enabled = false;
                            comboBox7.Enabled = false;
                            // 指示灯的状态显示
                            textBox5.Text = "ON";
                            // 显示串口连接正常
                            textBox2.AppendText("串口已连接\r\n"); // 显示串口已连接  
                            current = Value_table("current", comboBox1.Text);
                            connect_time = Value_table("connect_time", comboBox3.Text);
                            delay_time = Value_table("delay_time", comboBox5.Text);
                            baud = Value_table("Baud_ratio", comboBox4.Text);

                            // 发送测试信息帧
                            send_test_signal(current, delay_time, connect_time, baud);
                            // 定时器代码
                            build_timer();
                            timerTimer.Elapsed += Run;
                            timerTimer.AutoReset = false;
                            timerTimer.Enabled = true;
                        }
                        catch { MessageBox.Show("当前串口被占用或不存在，请重新选择");
                            if (serialPort1.IsOpen)
                                DisconnectDeveice();

                        }
                        
                        

                    }
                }                   

            }



        }
        
        void Run(object o,EventArgs e)
        {
            
            if (flag_timer1 == false)
            {   // 如果判断为超时，那么弹窗提醒，并直接停止测试
                MessageBox.Show("通信超时，请检查被测产品是否上电", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                textBox4.Text = "通信正常";
                label1.ForeColor = Color.Red;
                timerTimer.Enabled=false;
                button1.Enabled = true;
                button2.Enabled = false;
                comboBox1.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
                comboBox6.Enabled = true;
                comboBox7.Enabled = true;
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
               // textBox2.AppendText("测试已停止\r\n");
                if (serialPort1.IsOpen)
                    DisconnectDeveice(); 
                textBox5.Text = "OFF";
                
            }
            else 
            {
                // 如果没有超时，这时候定时器也需要关闭
                timerTimer.Close();
            }

        }

        // 前向差分运算
        public int[] ForwardDifference(int[] array)
        {
            int[] diff = new int[array.Length - 1];

            for (int i = 0; i < array.Length - 1; i++)
            {
                diff[i] = array[i + 1] - array[i];
            }

            return diff;
        }
        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
        
        // 停止测试按钮
        private void button2_Click(object sender, EventArgs e)
        {
           // 防止按下停止以后还在计时
            if (timerTimer.Enabled == true) {
                timerTimer.Enabled = false;
            }
           // 指示灯颜色变红
            label1.ForeColor = Color.Red; 
           // 可以重新选择参数并且重新发起测试
            button1.Enabled = true;
            button2.Enabled = false;
            comboBox1.Enabled = true;
            comboBox3.Enabled = true;
            comboBox4.Enabled = true;
            comboBox5.Enabled = true;
            comboBox6.Enabled = true;
            comboBox7.Enabled = true;
            radioButton1.Enabled = true;
            radioButton2.Enabled = true;
            // textBox2.AppendText("测试已停止\r\n");
            if (serialPort1.IsOpen)
                DisconnectDeveice();
            textBox5.Text = "OFF";

        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void chart1_Click_1(object sender, EventArgs e)
        {

        }

        private void chart4_Click(object sender, EventArgs e)
        {
            
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = dialog.SelectedPath;
                // 在此处处理所选文件夹路径
                select_path = folderPath;
                textBox3.Text = folderPath;

            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        
        // 串口数据接收部分
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)//串口数据接收 DataReceived
        {
            Thread.Sleep(150); // 为了保证数据接受完整，需要150Ms缓冲时间，非常重要
            // 防止死锁的代码
            if (m_IsTryToClosePort) // 关键！！！
            {
                return;
            }

            m_IsReceiving = true; // 关键!!!
            try
            {
                // int len = serialPort1.BytesToRead;//获取可以读取的字节数
                int[] buffer_store = new int[129];
                int len = 129;
                byte[] buffer = new byte[129];//创建缓存数据数组
                serialPort1.Read(buffer, 0, len);//把数据读取到buffer数组
                                                 //  Thread.Sleep(100);

                // 如果是response,则存储起来 
                if (buffer[0] == 239 && buffer[1] == 145 && buffer[2] == 126)
                {
                    timerTimer.Enabled = false;
                    for (int i = 0; i < 129; i++)
                    {
                        buffer_store[i] = buffer[i];
                    }
                }

                string strshow = Encoding.Default.GetString(buffer);//Byte值根据ASCII码表转为 String

                //封装一个方法 委托运行，不加会报错
                // BeginInvoke 不会造成界面的假死，但是Invoke可以
                BeginInvoke((new Action(() =>
                {
                //Thread.Sleep(100);
                //如果接收到了keep alive 信号
                if (buffer[0] == 239 && buffer[1] == 129 && buffer[2] == 2)
                    {
                        textBox4.Text = "通信正常";
                        //如果keep alive和 failed 拼接到一块了
                        if(buffer[5] == 239 && buffer[6] == 145 && buffer[7] == 2)
                        {
                            timerTimer.Enabled = false;
                            MessageBox.Show("测试失败，请重新发起测试");
                            textBox4.Text = "通信正常";
                            label1.ForeColor = Color.Red;
                            button1.Enabled = true;
                            button2.Enabled = false;
                            comboBox1.Enabled = true;
                            comboBox3.Enabled = true;
                            comboBox4.Enabled = true;
                            comboBox5.Enabled = true;
                            comboBox6.Enabled = true;
                            comboBox7.Enabled = true;
                            radioButton1.Enabled = true;
                            radioButton2.Enabled = true;
                            if (serialPort1.IsOpen)
                                DisconnectDeveice();
                            textBox5.Text = "OFF";
                        }
                    }

                // 单次测试的情况下接收到response消息无误 

                if (buffer_store[0] == 239 && buffer_store[1] == 145 && buffer_store[2] == 126 && radioButton1.Checked == true)

                    {
                        singal_count++;
                        flag_timer1 = true;
                        textBox4.Text = null;
                        Thread.Sleep(50);
                        textBox4.AppendText("通信正常");
                        int Interval_time = buffer_store[4] * 256 + buffer_store[3];
                        int signal_length = buffer_store[2];

                    //textBox2.AppendText("\r\n");//换行
                    // System.Threading.Thread.Sleep(50);  //等待50ms


                        current_amptitude = buffer_store[5];
                        current_phi = buffer_store[6];
                        voltage_amptitude = buffer_store[7];
                        voltage_phi = buffer_store[8];
                        //获取信号一
                        for (int i = 0; i < 120; i++)
                        {
                            Signal1[i] = buffer_store[9 + i];
                        }
                        // 前向差分获取信号二
                        Signal2 = ForwardDifference(Signal1);
                        // 清除上一次测试的图，不清除就会叠在一起
                        chart_init();
                        init_chart();
                        // 绘图
                        chart_plot(4, current_amptitude, current_phi);
                        chart_plot(3, voltage_amptitude, voltage_phi);
                        chart_plot_signal(1, Signal1);
                        chart_plot_signal(2, Signal2);
                        // 显示脱扣时间
                        textBox1.Text = "   " + Interval_time.ToString() + "ms";
                        textBox2.AppendText(singal_count.ToString() + "." + "检测时间:" + GetTimeStamp() + " " + "脱扣时长:" + Interval_time.ToString()+ "ms"+"\r\n" + "  " +"接通延时:"+comboBox5.Text+"  "+"接通时长:"+comboBox3.Text+"\r\n \r\n");//换行//换行


                    }
                // 接收到failed消息，直接停止测试

                if (buffer[0] == 239 && buffer[1] == 145 && buffer[2] == 2)
                    {
                        timerTimer.Enabled = false;
                        MessageBox.Show("测试失败，请重新发起测试");
                        textBox4.Text = "通信正常";
                        label1.ForeColor = Color.Red;
                        button1.Enabled = true;
                        button2.Enabled = false;
                        comboBox1.Enabled = true;
                        comboBox3.Enabled = true;
                        comboBox4.Enabled = true;
                        comboBox5.Enabled = true;
                        comboBox6.Enabled = true;
                        comboBox7.Enabled = true;
                        radioButton1.Enabled = true;
                        radioButton2.Enabled = true;
                        if (serialPort1.IsOpen)
                            DisconnectDeveice();
                        textBox5.Text = "OFF";
                    }
                // 连续测试情况和单次测试情况一致，只不过需要经过一个duration之后再次给下位机发送数据，开定时器，然后下位机返回数据
                if (buffer[0] == 239 && buffer[1] == 145 && buffer[2] == 126 && radioButton2.Checked == true)
                    {
                        //关定时器
                        flag_timer1 = true;
                        textBox4.Text = null;
                        Thread.Sleep(50);
                        textBox4.AppendText("通信正常");
                        // 记录是第几次测试
                        count++;
                        int Interval_time = buffer_store[4] * 256 + buffer_store[3];
                        int signal_length = buffer_store[2];

                        //textBox2.AppendText("\r\n");//换行
                        // System.Threading.Thread.Sleep(50);  //等待50ms


                        current_amptitude = buffer_store[5];
                        current_phi = buffer_store[6];
                        voltage_amptitude = buffer_store[7];
                        voltage_phi = buffer_store[8];
                        for (int i = 0; i < 120; i++)
                        {
                            Signal1[i] = buffer_store[9 + i];
                        }
                        Signal2 = ForwardDifference(Signal1);

                        
                        init_chart();
                        chart_init();

                        chart_plot(4, current_amptitude, current_phi);
                        chart_plot(3, voltage_amptitude, voltage_phi);
                        chart_plot_signal(1, Signal1);
                        chart_plot_signal(2, Signal2);

                        textBox1.Text = Interval_time.ToString() + "ms";

                        textBox2.AppendText( count.ToString() + "." + "检测时间：" + GetTimeStamp() + "\t" + "脱扣时长：" + Interval_time.ToString() + "ms\r\n \r\n");//换行
                        
                    // 延迟间隔时间
                    switch (comboBox7.Text)
                        {
                            case "1s":
                                System.Threading.Thread.Sleep(1000);
                                break;
                            case "3s":
                                System.Threading.Thread.Sleep(3000);
                                break;
                            case "5s":
                                System.Threading.Thread.Sleep(5000);
                                break;
                            case "10s":
                                System.Threading.Thread.Sleep(10000);
                                break;
                            case "15s":
                                System.Threading.Thread.Sleep(15000);
                                break;
                            case "30s":
                                System.Threading.Thread.Sleep(30000);
                                break;
                            case "60s":
                                System.Threading.Thread.Sleep(60000);
                                break;
                        }

                        current = Value_table("current", comboBox1.Text);
                        connect_time = Value_table("connect_time", comboBox3.Text);
                        delay_time = Value_table("delay_time", comboBox5.Text);
                        baud = Value_table("Baud_ratio", comboBox4.Text);

                    // 发送测试信息帧
                        send_test_signal(current, delay_time, connect_time, baud);
                        build_timer();
                        flag_timer1 = false;
                        timerTimer.Enabled = true;
                        timerTimer.Elapsed += Run;
                        timerTimer.AutoReset = false;


                    }



                // textBox1.AppendText(Convert.ToString(buffer,16));



            })));
            }
            finally
            {   // 这些进行以后，没有接收的动作，将这个flag设置为false
                m_IsReceiving = false;
            }
        }



        //

        private void button4_Click(object sender, EventArgs e)
        {

                
            
            // 该按钮实现功能为将textBox2中的文本保存在选定的文件夹下面
            try
            {
                save_path = select_path + '\\' + file_name;

                // 创建文件流将textbox2中的文本写入 txt文件中
                StreamWriter sw = new StreamWriter(save_path);
                string strTemp = textBox2.Text;
                sw.Write(strTemp);
                sw.Close();
                MessageBox.Show("记录文件已成功保存");
                singal_count = 0;
                textBox2.Text = null;
                textBox1.Text = null;
                // 防止按下停止以后还在计时
                if (timerTimer.Enabled == true)
                {
                    timerTimer.Enabled = false;
                }
                // 指示灯颜色变红
                label1.ForeColor = Color.Red;
                // 可以重新选择参数并且重新发起测试
                button1.Enabled = true;
                button2.Enabled = false;
                comboBox1.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
                comboBox6.Enabled = true;
                comboBox7.Enabled = true;
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                // textBox2.AppendText("测试已停止\r\n");
                if (serialPort1.IsOpen)
                    DisconnectDeveice();
                textBox5.Text = "OFF";

            }
            catch
            {
                MessageBox.Show("请进行实验后保存历史文件");
            }
            

        }
        // 绘制电流和电压的曲线
        private void chart_plot(int index, int Amplitude,double phi)
        {
            chart1.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            chart2.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            chart3.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            chart4.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            double [] x = new double[2400];
            x[0] = 0;
            for (int i = 1; i < 2400; i++)
            {
                x[i] = x[i - 1] + 0.1;
            }

                switch (index)
            {
                case 4:
                    foreach (double j in x )
                    {
                        
                        chart4.Series[0].Points.AddXY(j, Math.Sin(100 * j * Math.PI/1000 + phi) * (Amplitude+0.25));
                        // 绘制的线宽
                        chart4.Series[0].BorderWidth = 3;
                        chart4.Series[0].Color = Color.Red;
                        chart4.ChartAreas[0].AxisX.Title = "时间/ms";
                        chart4.ChartAreas[0].AxisY.Title = "电流值/A";
                        // 间隔线之间的间隔
                        chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart3.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart4.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart1.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart2.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart3.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart4.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        // 间隔线设置为虚线
                        chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart2.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart4.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                    }
                    break;
                case 3:
                    foreach (double j in x)
                    {
                        // 同上
                        chart3.Series[0].Points.AddXY(j, Math.Sin(100 * j * Math.PI/1000 + phi) * Amplitude);
                        chart3.Series[0].BorderWidth = 3;
                        chart3.Series[0].Color = Color.Green;
                        chart3.ChartAreas[0].AxisX.Title = "时间/ms";
                        chart3.ChartAreas[0].AxisY.Title = "电压值/V";
                        chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart3.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart4.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart1.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart2.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart3.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart4.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart2.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart4.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                    }
                    break;

            }

        }

        private void chart_plot_signal(int index ,int[] signal)
        {
            // 设定x轴的最小值
            chart1.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart2.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart3.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart4.ChartAreas[0].AxisX.IsStartedFromZero = true;
            int length;
            length = signal.Length;
            double[] x = new double[length];
            x[0] = 0;
            for (int i = 1; i < length; i++)
            {
                x[i] = x[i - 1] + 1;
            }

            switch (index)
            {
                case 1:
                    foreach (int j in x)
                    {
                        // 绘制 信号1，其余同电流、电压绘制
                        chart1.Series[0].Points.AddXY(2*j, signal[j]);
                        chart1.Series[0].BorderWidth = 3;
                        chart1.Series[0].Color = Color.Yellow;
                        chart1.ChartAreas[0].AxisX.Title = "时间/ms";
                        chart1.ChartAreas[0].AxisY.Title = "信号1";
                        chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart3.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart4.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart1.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart2.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart3.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart4.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart2.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart4.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                    }
                    break;
                case 2:
                    foreach (int j in x)
                    {
                        //同上
                        chart2.Series[0].Points.AddXY(2*j, signal[j]);
                        chart2.Series[0].BorderWidth = 3;
                        chart2.Series[0].Color = Color.White;
                        chart2.ChartAreas[0].AxisX.Title = "时间/ms";
                        chart2.ChartAreas[0].AxisY.Title = "信号2";
                        chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart2.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart3.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart4.ChartAreas[0].AxisX.MajorGrid.Interval = 20;
                        chart1.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart2.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart3.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart4.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
                        chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart2.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                        chart4.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                    }
                    break;

            }

        }

        private void init_chart()  // 图标初始化操作
        {
            // 清空所有的点
            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            chart3.Series[0].Points.Clear();
            chart4.Series[0].Points.Clear();
            chart1.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart2.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart3.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart4.ChartAreas[0].AxisX.IsStartedFromZero = true;
        }
            private void chart3_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.Text == "开")
            {
                // 如果开的话，背景线的表格线保持开，颜色为白色

                chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
                chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;
                chart2.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
                chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;
                chart3.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
                chart3.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;
                chart4.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
                chart4.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;

            }
            else {
                // 为关的话，背景线为黑色，也就是看不见
                chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Black;
                chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Black;
                chart2.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Black;
                chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Black;
                chart3.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Black;
                chart3.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Black;
                chart4.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Black;
                chart4.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Black;
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
        private void chart_init()
        {
            //初始化设定图像的一些特征：背景、横纵坐标等等
            chart3.ChartAreas[0].AxisX.Title = "时间/ms";
            chart3.ChartAreas[0].AxisY.Title = "电压值/V";
            chart4.ChartAreas[0].AxisX.Title = "时间/ms";
            chart4.ChartAreas[0].AxisY.Title = "电流值/A";
            chart2.ChartAreas[0].AxisX.Title = "时间/ms";
            chart2.ChartAreas[0].AxisY.Title = "信号2";
            chart1.ChartAreas[0].AxisX.Title = "时间/ms";
            chart1.ChartAreas[0].AxisY.Title = "信号1";
            chart1.ChartAreas[0].BackColor = Color.Black;
            chart2.ChartAreas[0].BackColor = Color.Black;
            chart3.ChartAreas[0].BackColor = Color.Black;
            chart4.ChartAreas[0].BackColor = Color.Black;
            chart1.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            chart2.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            chart3.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值
            chart4.ChartAreas[0].AxisX.Minimum = 0;//设定x轴的最小值

            chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart2.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart4.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            // 设置x轴最大值
            chart1.ChartAreas[0].AxisX.Maximum = 240;
            chart2.ChartAreas[0].AxisX.Maximum = 240;
            chart3.ChartAreas[0].AxisX.Maximum = 240;
            chart4.ChartAreas[0].AxisX.Maximum = 240;
        }
        private void component_init()
        {
            // 设置char初始化
            
            comboBox1.Text = "3A";
            comboBox5.Text = "3s";
            comboBox3.Text = "1s";
            comboBox4.Text = "9600";
            comboBox2.Text = "关";
            comboBox6.Text = "COM1";

            chart_init();
            radioButton1.Checked = true;
            textBox4.Text = "未连接";
            textBox5.Text = "WAIT";
            //连续模式中 单次测试和连续测试的间隔时间设置
            if (radioButton1.Checked == true)
            {
                comboBox7.Text = "无";
            }
            else
            {
                comboBox7.Text = "5s";
            }         
            

        }
        public int Value_table(string name,string value)
            //这段代码主要是查表，建立起对应关系
        {
            int level= 0 ;
            switch (name) {
                case "current":
                    switch(value)
                    {
                        case "3A":
                            level = 1;
                            break;
                        case "6A":
                            level = 2;
                            break;
                        case "13A":
                            level = 3;
                            break;
                        case "20A":
                            level = 4;
                            break;
                        case "40A":
                            level = 5;
                            break;
                        case "63A":
                            level = 6;
                            break;
                        case "75A":
                            level = 7;
                            break;
                    }
                    break;
                case "delay_time":
                    switch (value)
                    {
                        case "0s":
                            level = 1;
                            break;
                        case "0.5s":
                            level = 2;
                            break;
                        case "1s":
                            level = 3;
                            break;
                        case "3s":
                            level = 4;
                            break;
                        case "10s":
                            level = 5;
                            break;
                        case "20s":
                            level = 6;
                            break;
                        case "30s":
                            level = 7;
                            break;
                    }
                    break;
                case "connect_time":
                    switch (value)
                    {
                        case "0.3s":
                            level = 1;
                            break;
                        case "0.5s":
                            level = 2;
                            break;
                        case "1s":
                            level = 3;
                            break;
                        case "2s":
                            level = 4;
                            break;
                        case "3s":
                            level = 5;
                            break;
                        case "5s":
                            level = 6;
                            break;
                        case "10s":
                            level = 7;
                            break;
                    }
                    break;
                case "Baud_ratio":
                    switch (value)
                    {
                        case "2400":
                            level = 1;
                            break;
                        case "4800":
                            level = 2;
                            break;
                        case "9600":
                            level = 3;
                            break;
                        case "19200":
                            level = 4;
                            break;
                        case "38400":
                            level = 5;
                            break;
                        case "57600":
                            level = 6;
                            break;
                        case "115200":
                            level = 7;
                            break;
                    }
                    break;

                case "duration":
                    switch (value)
                    {
                        case "无":
                            level = 0;
                            break;
                        case "1s":
                            level = 1000;
                            break;
                        case "3s":
                            level = 2000;
                            break;
                        case "5s":
                            level = 3000;
                            break;
                        case "10s":
                            level = 4000;
                            break;
                        case "15s":
                            level = 5000;
                            break;
                        case "30s":
                            level = 6000;
                            break;
                        case "60s":
                            level = 7000;
                            break;
                    }
                    break;
            }

            return level;
        }
        // 端口初始化设定
        private void port_init() 
        {
            // 设置属性 
            serialPort1.PortName = comboBox6.Text;
            // 波特率设置
            serialPort1.BaudRate = int.Parse(comboBox4.Text);
            // 八个数据位
            serialPort1.DataBits = 8;
            // 一个停止位
            serialPort1.StopBits = StopBits.One;
            // 无校验位
            serialPort1.Parity = Parity.None;
            

        }
        public static string GetTimeStamp()
        {
            // 获取当前时间，用于连续模式下的显示
            // TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            //  return Convert.ToInt64(ts.TotalSeconds).ToString();

            //   DateTime.Now.ToString();            // 2008-9-4 20:02:10
            return DateTime.Now.ToLocalTime().ToString();        // 2008-9-4 20:12:12
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox7.Text = "无";
        }
        private void send_test_signal(int current, int delay,int connect,int baud) {
            // 发送test信号
            Thread.Sleep(500);
            List<byte> sendBytes = new List<byte>();
            sendBytes.Add(0xEF);
            sendBytes.Add(0x90);
            sendBytes.Add(0x04);
            // 对应关系发送不同的字节
            switch (current)
            {
                case 1:
                    sendBytes.Add(0x01);
                    break;
                case 2:
                    sendBytes.Add(0x02);
                    break;
                case 3:
                    sendBytes.Add(0x03);
                    break;
                case 4:
                    sendBytes.Add(0x04);
                    break;
                case 5:
                    sendBytes.Add(0x05);
                    break;
                case 6:
                    sendBytes.Add(0x06);
                    break;
                case 7:
                    sendBytes.Add(0x07);
                    break;
            }
            switch (delay)
            {
                case 1:
                    sendBytes.Add(0x01);
                    break;
                case 2:
                    sendBytes.Add(0x02);
                    break;
                case 3:
                    sendBytes.Add(0x03);
                    break;
                case 4:
                    sendBytes.Add(0x04);
                    break;
                case 5:
                    sendBytes.Add(0x05);
                    break;
                case 6:
                    sendBytes.Add(0x06);
                    break;
                case 7:
                    sendBytes.Add(0x07);
                    break;
            }
            switch (connect)
            {
                case 1:
                    sendBytes.Add(0x01);
                    break;
                case 2:
                    sendBytes.Add(0x02);
                    break;
                case 3:
                    sendBytes.Add(0x03);
                    break;
                case 4:
                    sendBytes.Add(0x04);
                    break;
                case 5:
                    sendBytes.Add(0x05);
                    break;
                case 6:
                    sendBytes.Add(0x06);
                    break;
                case 7:
                    sendBytes.Add(0x07);
                    break;
            }
            switch (baud)
            {
                case 1:
                    sendBytes.Add(0x01);
                    break;
                case 2:
                    sendBytes.Add(0x02);
                    break;
                case 3:
                    sendBytes.Add(0x03);
                    break;
                case 4:
                    sendBytes.Add(0x04);
                    break;
                case 5:
                    sendBytes.Add(0x05);
                    break;
                case 6:
                    sendBytes.Add(0x06);
                    break;
                case 7:
                    sendBytes.Add(0x07);
                    break;
            }
            serialPort1.Write(sendBytes.ToArray(), 0, sendBytes.Count);

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            comboBox7.Text = "5s";
        }
        //防止死锁的核心，和peterson算法的内涵一样
        public void DisconnectDeveice() // 关键和核心！！！
        {
            m_IsTryToClosePort = true;
            while (m_IsReceiving)
            {
                System.Windows.Forms.Application.DoEvents();
            }
            serialPort1.Close();
        }

        // 定时器线程创建
        public void build_timer()
        {
            int delay_timer=0;
            int duration_timer=0;
            switch (comboBox5.Text)
            {
                case "0s":
                    delay_timer = 3000;
                    break;
                case "0.5s":
                    delay_timer = 3000;
                    break;
                case "1s":
                    delay_timer = 3000;
                    break;
                case "3s":
                    delay_timer = 6000;
                    break;
                case "10s":
                    delay_timer = 10000;
                    break;
                case "20s":
                    delay_timer = 20000;
                    break;
                case "30s":
                    delay_timer = 30000;
                    break;
            }
            switch (comboBox3.Text)
            {
                case "0.3s":
                    duration_timer = 3000;
                    break;
                case "0.5s":
                    duration_timer = 3000;
                    break;
                case "1s":
                    duration_timer = 3000;
                    break;
                case "2s":
                    duration_timer = 4000;
                    break;
                case "3s":
                    duration_timer = 5000;
                    break;
                case "5s":
                    duration_timer = 7000;
                    break;
                case "10s":
                    duration_timer = 12000;
                    break;
            }
            int timer_all = delay_timer + duration_timer;
            // 给定时器创建一个新的线程
            timerTimer = new System.Timers.Timer(timer_all);

        }

    }


}
