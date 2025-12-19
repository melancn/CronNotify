using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Njson = Newtonsoft.Json;

namespace CronNotify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private int run_id = 0;
        private Boolean is_stop = false;


        public MainWindow()
        {
            InitializeComponent();

            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipText = "系统监控中... ...";
            this.notifyIcon.ShowBalloonTip(2000);
            this.notifyIcon.Text = "系统监控中... ...";
            this.notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            this.notifyIcon.Visible = true;


            //this.notifyIcon.MouseClick += NotifyIcon_MouseClick;
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler((o, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (this.Visibility == System.Windows.Visibility.Visible) { this.HideWindow(); }
                    else this.ShowWindow();
                }
            });

            addIconMenu();
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //addIconMenu();
            }
        }

        private void addIconMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("退出", null, (s, e) => Close());

            notifyIcon.ContextMenuStrip = menu;
        }

        private void ShowWindow()
        {
            this.Show();
            WindowState = WindowState.Normal;
            this.Activate();
        }

        private void HideWindow()
        {
            this.Hide();
            notifyIcon.ShowBalloonTip(3000, "监控", "监控最小化到托盘", ToolTipIcon.Info);
        }

        private void CloseWindow()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            //notifyIcon = null;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized) { this.HideWindow(); } else this.ShowWindow();

        }
        public void addTextToInfo(string a)
        {
            Dispatcher.InvokeAsync(() =>
            {
                string date = DateTime.Now.ToString("u");
                info.Text = info.Text + date + "\r\n" + a + "\r\n";
                info.ScrollToVerticalOffset(info.ExtentHeight);
            }
            );
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ct.Text))
            {
                System.Windows.MessageBox.Show("任务不能空");
                return;
            }
            if (run_id > 0)
            {
                timeblock.Text = "正在停止...";
                is_stop = true;
            }
            else
            {
                timeblock.Text = "运行中";
                start.Content = "停止";
                processWatching(ct.Text);
            }
        }

        private async void processWatching(string content)
        {
            List<TimeList>? lists = null;
            try
            {
                lists = Njson.JsonConvert.DeserializeObject<List<TimeList>>(content, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-dd HH:mm:ss"
                });
                Debug.WriteLine(lists);
            } catch (Exception e)
            {
                timeblock.Text = "";
                start.Content = "开始";
                System.Windows.MessageBox.Show(e.Message);
            }
            if (lists == null)
            {
                return;
            }
            lists.Sort();
            StringBuilder? info = new StringBuilder("\r\n");
            foreach (TimeList list in lists)
            {
                info.AppendLine(list.time.ToString("yyyy-MM-dd HH:mm:ss") + " 【" + list.text + "】 " + list.beforeSecond);
            }
            addTextToInfo("获取任务：" + info);
            info = null;
            this.ct2.DataContext = lists;
            await System.Threading.Tasks.Task.Run(() =>
            {
                Debug.WriteLine("s Thread ID:#{0}", Thread.CurrentThread.ManagedThreadId);
                run_id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                DateTime now;

                List < TimeList > nlist;
                while (true)
                {
                    int len = lists.Count;
                    if (is_stop|| len==0)
                    {
                        run_id = 0;
                        is_stop = false;
                        System.Windows.MessageBox.Show("已停止");

                        Dispatcher.InvokeAsync(() =>
                        {
                            start.Content = "开始";
                            timeblock.Text = "";
                        });
                        break;
                    }
                    now = DateTime.Now;
                    nlist = new List<TimeList>();
                    foreach (var list in lists)
                    {
                        Debug.WriteLine(list.time);
                        if (list.time <= now.AddSeconds(-300))
                        {
                            continue;
                        }
                        else if (list.time <= now)
                        {
                            addTextToInfo("提醒：" + list.text);
                            if (notifyIcon != null) notifyIcon.ShowBalloonTip(100, "", list.text, ToolTipIcon.Info);
                            continue;

                        }
                        else if (list.times == 0 && list.time.AddSeconds(-list.beforeSecond) <= now)
                        {
                            addTextToInfo("提前提醒：" + list.time + " " + list.text);
                            if (notifyIcon != null) notifyIcon.ShowBalloonTip(100, "", list.text, ToolTipIcon.Info);
                            list.times++;
                        }
                        nlist.Add(list);
                    }
                    if (lists.Count != nlist.Count)
                    {
                        string text = Njson.JsonConvert.SerializeObject(nlist, new JsonSerializerSettings
                        {
                            DateFormatString = "yyyy-MM-dd HH:mm:ss"
                        });
                        //Dispatcher.InvokeAsync(() =>
                        //{
                        //    ct2.DataContext = text;
                        //});
                        lists = nlist;
                    }
                    Debug.WriteLine(lists.Count);


                    Thread.Sleep(1000);
                }
            });
        }
    }
}
